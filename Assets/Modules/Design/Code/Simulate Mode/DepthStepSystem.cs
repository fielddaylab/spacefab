using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-depth flow-propagation worker. Only acts on frames where SimulateModeSystem flagged
    /// PaintDepthThisFrame = true and the phase is Propagating. Walks the edges in OrderedEdges
    /// at runState.CurrentDepth, computes flow per edge (diode gating, gate-above inversion,
    /// cycle handling), and writes FlowState / TempTransformation onto GridCells along each
    /// edge's path. On the final depth it also settles the whole edge list to a fixed point
    /// before the row is scored. Does not tick time or manage phase.
    /// Runs on Update at order 2 under SimulateModeMask.
    /// </summary>
    public class DepthStepSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 2, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateRunState>()
                    .ReadWriteShared<SimulateRunScratch>()
                    .ReadShared<SimulateGraphState>()
                    .ReadShared<GridStackState>()
                    .ReadWriteShared<VisualGridStackState>()
            );
        }

        // Paints the current depth's edges when signalled by SimulateModeSystem.
        //
        // Per-edge algorithm:
        //   1. Determine the flow entering this edge. For Input origins, read from the per-test
        //      InputFlowByNode array primed in ProcessPreparingTest. For non-Input origins, read
        //      the origin's current NodeFlow.
        //   2. Diode gating. If both endpoints are transistor cells of opposite polarity (taking
        //      temp-transform inversions into account), flow only passes P→N for HI signals.
        //   3. GateAbove inversion. If the destination is a GateAbove, the incoming signal can
        //      flip the transistor type on the matching below-cell: HI inverts P→N, LO inverts
        //      N→P, Unstable leaves it alone.
        //   4. Stability check. If the destination already has a non-empty flow from a prior
        //      edge this test, the two must match — else unstable. Also unstable if the edge
        //      is marked CycleDetected by the graph-build cycle-detection pass.
        //   5. Apply. Write the resulting flow into runScratch.NodeFlow[OtherIndex] and paint
        //      every path cell via SimulateRunScratchUtility.SetCellFlow.
        //
        // On the last depth, RunConvergenceSweeps then drives the full edge list to a fixed
        // point so a driver that arrived late still propagates. That settling is instantaneous —
        // it shares this frame with the final depth's paint rather than adding a phase.
        //
        // One VisualsNeedRefreshing write at the end covers the whole depth's worth of paints;
        // GridVisualsUpdateSystem handles the actual renderer update in LateUpdate.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out SimulateRunState runState,
                out SimulateRunScratch runScratch,
                out SimulateGraphState graphState,
                out GridStackState gridStackState
                );
            VisualGridStackState visualState = Find.State<VisualGridStackState>();

            // Gate: only act on frames where ProcessPropagating flagged a depth-paint.
            if (!runState.PaintDepthThisFrame) { return; }
            if (runState.Phase != SimulatePhase.Propagating) { return; }

            int currDepth = runState.CurrentDepth;
            // DepthEdgeStart is sized (MaxDepth + 2). Guard against CurrentDepth overflow
            // (shouldn't happen — ProcessPropagating caps at MaxDepth — but defensive).
            if (graphState.DepthEdgeStart == null || currDepth + 1 >= graphState.DepthEdgeStart.Length)
            {
                return;
            }

            int startIdx = graphState.DepthEdgeStart[currDepth];
            int endIdx = graphState.DepthEdgeStart[currDepth + 1];

            Dimensions dims = gridStackState.GridStack.LayerDims;
            int numCols = dims.X;
            int cellsPerLayer = numCols * dims.Y;

            for (int e = startIdx; e < endIdx; e++)
            {
                ProcessEdge(graphState.OrderedEdges[e], runState, runScratch, graphState, gridStackState, numCols, cellsPerLayer);
            }

            // The depth walk has now visited every edge exactly once, in depth order. Settle the
            // result before the row is scored — see RunConvergenceSweeps for why one ordered pass
            // isn't enough.
            if (currDepth == graphState.MaxDepth)
            {
                RunConvergenceSweeps(runState, runScratch, graphState, gridStackState, numCols, cellsPerLayer);
            }

            // Paint connected cells that lie on no crucial-to-crucial path (dead-end "stub"
            // branches). Each such cell mirrors the resolved flow of its representative crucial
            // cell (assigned in SimulateGraphUtility Pass 6, never crossing a P-N boundary). A
            // representative still reading Empty this depth leaves its stub unpainted — so a stub
            // lights up the same depth its segment does, and a stub off an undriven/diode-blocked
            // segment stays grey. Pure visual coverage: NodeFlow / output values are untouched.
            for (int i = 0; i < graphState.RepresentedCellCount; i++)
            {
                int cellIdx = graphState.RepresentedCells[i];
                int repCellIdx = graphState.CellFlowRepresentative[cellIdx];
                FlowState repFlow = SimulateRunScratchUtility.GetCellFlow(runScratch, repCellIdx);
                if (repFlow != FlowState.Empty)
                {
                    SimulateRunScratchUtility.SetCellFlow(runScratch, cellIdx, repFlow);
                }
            }

            // One visual refresh per depth boundary, covering every cell painted this frame.
            visualState.VisualsNeedRefreshing = true;
        }

        // Re-runs every edge until no per-node value changes, then leaves the graph settled.
        //
        // Why one depth-ordered pass isn't enough: a node's outgoing edges are stamped with the
        // depth at which the node was DISCOVERED. A second driver that reaches that node later
        // writes its flow after those outgoing edges have already been evaluated, so the
        // contribution stops dead. Iterating to a fixed point makes the settled result
        // independent of both depth assignment and edge order within a depth: two drivers on one
        // region either agree or resolve Unstable, no matter which branch is shorter.
        //
        // Terminates because NodeFlow only ever moves Empty → value → Unstable and
        // NodeTempTransform only NONE → the one polarity its physical cell type admits: three
        // state advances per node at worst, and a sweep that changes nothing ends the loop. That
        // monotonicity is load-bearing — it is why step 4 of ProcessEdge compares against the
        // incoming flowState rather than NodeFlow[OriginIndex]. In practice one sweep over the
        // whole edge list settles most of the graph, so the realistic count is one or two; the
        // cap exists to catch a future rule change that breaks monotonicity rather than hang.
        static private void RunConvergenceSweeps(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, GridStackState gridStackState, int numCols, int cellsPerLayer)
        {
            int maxSweeps = 3 * graphState.NodeCount + 1;

            for (int sweep = 0; sweep < maxSweeps; sweep++)
            {
                bool changed = false;
                for (int e = 0; e < graphState.EdgeCount; e++)
                {
                    // Deliberately not short-circuiting: every edge must run every sweep.
                    changed |= ProcessEdge(graphState.OrderedEdges[e], runState, runScratch, graphState, gridStackState, numCols, cellsPerLayer);
                }

                if (!changed) { return; }
            }

            Debug.LogWarning("[DepthStepSystem] convergence sweeps hit the " + maxSweeps + " iteration cap; flow may not be settled");
        }

        // Process a single crucial edge. Splits out from ProcessWork so the inner branches can
        // be read top-to-bottom without the surrounding loop noise.
        //
        // Returns true if it changed any per-node value (NodeFlow or NodeTempTransform). That's
        // what RunConvergenceSweeps tests for a fixed point; per-cell paints are derived from
        // those values, so they settle once the node values do.
        static private bool ProcessEdge(CrucialEdge edge, SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, GridStackState gridStackState, int numCols, int cellsPerLayer)
        {
            bool changed = false;

            CrucialNode originNode = graphState.CrucialNodes[edge.OriginIndex];
            CrucialNode destNode = graphState.CrucialNodes[edge.OtherIndex];
            GridCell originCell = GridStackUtility.GetCellDirect(gridStackState, originNode.Coord);
            GridCell destCell = GridStackUtility.GetCellDirect(gridStackState, destNode.Coord);

            // --- Step 1: determine flow entering this edge ----------------------------------
            FlowState flowState;
            if (originCell.CellType == CellType.Input)
            {
                flowState = runScratch.InputFlowByNode[edge.OriginIndex];
            }
            else
            {
                flowState = runScratch.NodeFlow[edge.OriginIndex];
            }

            bool flowThrough = true;
            bool stable = flowState != FlowState.Unstable;

            // --- Step 2: diode gating (both endpoints are transistor cells) -----------------
            //
            // Effective type respects temp-transform (GateAbove inversion from earlier this
            // test may have flipped the polarity). If both effective types differ, we have a
            // P-N junction: flow only passes P→N when signal is HI.
            if (IsTransistorType(originCell.CellType) && IsTransistorType(destCell.CellType))
            {
                CellType originType = originCell.CellType;
                CellType originTemp = runScratch.NodeTempTransform[edge.OriginIndex];
                if (originTemp != CellType.NONE) { originType = originTemp; }

                CellType destType = destCell.CellType;
                CellType destTemp = runScratch.NodeTempTransform[edge.OtherIndex];
                if (destTemp != CellType.NONE) { destType = destTemp; }

                if (originType != destType)
                {
                    flowThrough = EvaluateFlowThroughDiode(flowState, originType, destType);
                }
            }

            // --- Step 3: GateAbove inversion ------------------------------------------------
            //
            // When the dest is a GateAbove, its signal reaches down to the matching cell on
            // the transistor layer (same col/row) and inverts its type. We update both the
            // per-node temp-transform (for future edges whose endpoints touch that node) and
            // the per-cell temp-transform (for visuals).
            if (destCell.TransferType == TransferType.GateAbove)
            {
                int belowLayer = (int)StackLayer.Transistor;
                int belowCellIdx = SimulateRunScratchUtility.CellIndex(belowLayer, destNode.Coord.Col, destNode.Coord.Row, numCols, cellsPerLayer);
                int belowCrucialIdx = graphState.CellToCrucial[belowCellIdx];
                GridCell belowCell = GridStackUtility.GetCellDirect(gridStackState, belowLayer, destNode.Coord.Col, destNode.Coord.Row);

                CellType newTransform = CellType.NONE;
                if (flowState == FlowState.Hi && belowCell.CellType == CellType.PTransistor)
                {
                    newTransform = CellType.NTransistor;
                }
                else if (flowState == FlowState.Lo && belowCell.CellType == CellType.NTransistor)
                {
                    newTransform = CellType.PTransistor;
                }
                // Unstable: no inversion, matches prototype behavior.

                if (newTransform != CellType.NONE)
                {
                    if (belowCrucialIdx >= 0 && runScratch.NodeTempTransform[belowCrucialIdx] != newTransform)
                    {
                        runScratch.NodeTempTransform[belowCrucialIdx] = newTransform;
                        changed = true;
                    }
                    SimulateRunScratchUtility.SetCellTempTransform(runScratch, belowCellIdx, newTransform);
                }
            }

            // --- Step 4: stability check ----------------------------------------------------
            //
            // If the destination already has a non-empty flow from a prior edge this test AND
            // the incoming flow is also non-empty, the two must match. Mismatch = unstable.
            // Additionally flag as unstable if the edge is part of a detected cycle.
            //
            // Compares the incoming flowState, not NodeFlow[OriginIndex]. For a non-Input origin
            // the two are the same value. For an Input origin NodeFlow is never written, so
            // reading it would skip this check entirely — an Input could then overwrite a
            // destination another driver had already resolved, letting two conflicting drivers
            // trade the same node back and forth on every convergence sweep instead of settling
            // on Unstable. Making the comparison against flowState is also what lets two Inputs
            // shorted to one node register as unstable at all.
            if (runScratch.NodeFlow[edge.OtherIndex] != FlowState.Empty
                && flowState != FlowState.Empty)
            {
                stable = flowState == runScratch.NodeFlow[edge.OtherIndex];
            }
            if (edge.CycleDetected) { stable = false; }

            // --- Step 5: apply flow + path paint --------------------------------------------
            if (!flowThrough) { return changed; }

            if (!stable)
            {
                if (runScratch.NodeFlow[edge.OriginIndex] != FlowState.Unstable)
                {
                    runScratch.NodeFlow[edge.OriginIndex] = FlowState.Unstable;
                    changed = true;
                }
                if (runScratch.NodeFlow[edge.OtherIndex] != FlowState.Unstable)
                {
                    runScratch.NodeFlow[edge.OtherIndex] = FlowState.Unstable;
                    changed = true;
                }
                flowState = FlowState.Unstable;
                runState.IsUnstable = true;
                runScratch.IsUnstable = true;
            }

            if (flowState == FlowState.Empty) { return changed; }

            // Write dest node's flow. Origin node's flow stays as-is — only writes when
            // stability forced Unstable above (handled by the `if (!stable)` branch).
            if (runScratch.NodeFlow[edge.OtherIndex] != flowState)
            {
                runScratch.NodeFlow[edge.OtherIndex] = flowState;
                changed = true;
            }

            // Paint every path cell with this flow. Path cells span origin → dest (inclusive).
            int pathStart = edge.PathStart;
            int pathEnd = pathStart + edge.PathLength;
            for (int p = pathStart; p < pathEnd; p++)
            {
                int cellIdx = graphState.PathPool[p];
                SimulateRunScratchUtility.SetCellFlow(runScratch, cellIdx, flowState);
            }

            return changed;
        }

        // True if the cell type is one of the two transistor polarities.
        static private bool IsTransistorType(CellType type)
        {
            return type == CellType.NTransistor || type == CellType.PTransistor;
        }

        // Returns true if signal can pass through a P-N junction. Signals flow P→N only, and
        // only on HI (or Unstable, which propagates through any direction). Matches prototype
        // EvaluateFlowThroughDiode at EvaluationMgr.cs:1046-1060.
        static private bool EvaluateFlowThroughDiode(FlowState flow, CellType originType, CellType destType)
        {
            if (flow != FlowState.Hi && flow != FlowState.Unstable) { return false; }
            return originType == CellType.PTransistor && destType == CellType.NTransistor;
        }
    }
}

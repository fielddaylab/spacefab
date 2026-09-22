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
        //   1. Read the flow entering this edge from the origin's SEGMENT. Inputs are not a
        //      special case — ProcessPreparingTest seeds their segments before propagation.
        //   2. Diode gating. If both endpoints are transistor cells of opposite polarity (taking
        //      temp-transform inversions into account), flow only passes P→N for HI signals.
        //   3. GateAbove inversion. If the destination is a GateAbove, the incoming signal can
        //      flip the transistor type on the matching below-cell: HI inverts P→N, LO inverts
        //      N→P, Unstable leaves it alone.
        //   4. Merge into the destination segment via AssignSegmentFlow, which resolves a
        //      conflicting second driver to Unstable, and re-sync that segment's revealed cells.
        //   5. Reveal the path: each path cell takes its own segment's colour.
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
            // isn't enough — then fill in every cell the walk never happened to reveal, so each
            // live segment finishes uniform rather than showing only the cells edges passed over.
            if (currDepth == graphState.MaxDepth)
            {
                RunConvergenceSweeps(runState, runScratch, graphState, gridStackState, numCols, cellsPerLayer);
                FillLiveSegments(runScratch, graphState);
            }

            // One visual refresh per depth boundary, covering every cell painted this frame.
            visualState.VisualsNeedRefreshing = true;
        }

        // Re-runs every edge until no segment or node value changes, leaving the graph settled.
        //
        // Why one depth-ordered pass isn't enough: a node's outgoing edges are stamped with the
        // depth at which the node was DISCOVERED. A second driver that reaches that node later
        // writes its flow after those outgoing edges have already been evaluated, so the
        // contribution stops dead. Iterating to a fixed point makes the settled result
        // independent of both depth assignment and edge order within a depth: two drivers on one
        // region either agree or resolve Unstable, no matter which branch is shorter.
        //
        // Terminates because SegmentFlow only ever moves Empty → value → Unstable (two advances
        // per segment) and NodeTempTransform only NONE → the one polarity its physical cell type
        // admits (one per node), and a sweep that changes nothing ends the loop. In practice one
        // sweep over the whole edge list settles most of the graph, so the realistic count is one
        // or two; the cap exists to catch a future rule change that breaks that monotonicity
        // rather than hang the frame.
        static private void RunConvergenceSweeps(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, GridStackState gridStackState, int numCols, int cellsPerLayer)
        {
            int maxSweeps = 2 * graphState.SegmentCount + graphState.NodeCount + 1;

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

        // Paints every cell of every segment holding a value. The walk only reveals cells that
        // happen to lie on a crucial-to-crucial path, which leaves stubs and far branches dark
        // even though they are the same conductor; this is what makes a region finish uniform.
        // Segments still reading Empty are left alone so undriven and diode-blocked wire stays grey.
        static private void FillLiveSegments(SimulateRunScratch runScratch, SimulateGraphState graphState)
        {
            for (int s = 0; s < graphState.SegmentCount; s++)
            {
                FlowState flow = runScratch.SegmentFlow[s];
                if (flow == FlowState.Empty) { continue; }

                int start = graphState.SegmentCellStart[s];
                int end = graphState.SegmentCellStart[s + 1];
                for (int i = start; i < end; i++)
                {
                    SimulateRunScratchUtility.SetCellFlow(runScratch, graphState.SegmentCells[i], flow);
                }
            }
        }

        // Recolours the cells of one conductor that the walk has ALREADY revealed this test,
        // leaving cells it has not reached yet dark so the crawl can keep advancing into them.
        // Called the step two painted fronts touch: a short is a whole-conductor event, so both
        // arms behind the meeting point recolour together rather than only the cell they met on.
        //
        // "Revealed" needs no extra bookkeeping: a matching CellFlowStamps entry already means the
        // cell was painted this test.
        static private void FloodRevealedCells(SimulateRunScratch runScratch, SimulateGraphState graphState, int segmentId, FlowState flow)
        {
            int start = graphState.SegmentCellStart[segmentId];
            int end = graphState.SegmentCellStart[segmentId + 1];
            for (int i = start; i < end; i++)
            {
                int cellIdx = graphState.SegmentCells[i];
                if (runScratch.CellFlowStamps[cellIdx] != runScratch.CurrentFlowStamp) { continue; }
                SimulateRunScratchUtility.SetCellFlow(runScratch, cellIdx, flow);
            }
        }

        // Process a single crucial edge. Splits out from ProcessWork so the inner branches can
        // be read top-to-bottom without the surrounding loop noise.
        //
        // Returns true if it moved any segment or node value. That's what RunConvergenceSweeps
        // tests for a fixed point; per-cell paints are derived from those values, so they settle
        // once the segment values do.
        static private bool ProcessEdge(CrucialEdge edge, SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, GridStackState gridStackState, int numCols, int cellsPerLayer)
        {
            bool changed = false;

            CrucialNode originNode = graphState.CrucialNodes[edge.OriginIndex];
            CrucialNode destNode = graphState.CrucialNodes[edge.OtherIndex];
            GridCell originCell = GridStackUtility.GetCellDirect(gridStackState, originNode.Coord);
            GridCell destCell = GridStackUtility.GetCellDirect(gridStackState, destNode.Coord);

            int originSegment = graphState.CrucialSegment[edge.OriginIndex];
            int destSegment = graphState.CrucialSegment[edge.OtherIndex];

            // --- Step 1: determine flow entering this edge ----------------------------------
            //
            // Always the origin's segment. Inputs need no special case: ProcessPreparingTest seeds
            // each Input's segment with the row's value before propagation starts.
            FlowState flowState = runScratch.SegmentFlow[originSegment];

            bool flowThrough = true;

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
                // An Unstable gate signal inverts nothing — there is no defined polarity to apply.

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

            // --- Step 4: apply flow to the destination segment ------------------------------
            //
            // AssignSegmentFlow carries the whole conflict rule: Empty takes the value, a matching
            // value is a no-op, a differing value resolves Unstable, and Unstable absorbs. Two
            // drivers on one conductor therefore agree or go Unstable regardless of the order
            // their edges happen to run in.
            //
            // An edge into a gate whose dependency never resolved is unstable by construction.
            if (!flowThrough) { return changed; }
            if (edge.CycleDetected) { flowState = FlowState.Unstable; }
            if (flowState == FlowState.Empty) { return changed; }

            changed |= SimulateRunScratchUtility.AssignSegmentFlow(runScratch, destSegment, flowState);

            if (runScratch.SegmentFlow[destSegment] == FlowState.Unstable)
            {
                runState.IsUnstable = true;
                runScratch.IsUnstable = true;
            }

            // --- Step 5: advance the crawl --------------------------------------------------
            //
            // Deliberately NOT reading SegmentFlow. A conductor resolves Unstable the instant a
            // conflicting driver crosses into it, but at that moment the two PAINTED fronts can
            // still be several unrevealed cells apart — colouring from the segment would turn the
            // region red while the currents are visibly nowhere near each other.
            //
            // Instead the crawl carries the colour already on the origin CELL, so a front keeps
            // advancing in its own colour until it physically runs into another one. An unpainted
            // origin means no current has visibly arrived there yet, so there is nothing to
            // advance; the electrical work above still happened.
            FlowState crawlFlow = SimulateRunScratchUtility.GetCellFlow(runScratch, originNode.CellIndex);
            if (crawlFlow == FlowState.Empty) { return changed; }

            int pathStart = edge.PathStart;
            int pathEnd = pathStart + edge.PathLength;
            for (int p = pathStart; p < pathEnd; p++)
            {
                int cellIdx = graphState.PathPool[p];

                // A cell already holding a DIFFERENT colour is where two fronts just touched —
                // the moment the currents actually meet, which lands later than the electrical
                // conflict AssignSegmentFlow recorded. Recolour everything revealed on that
                // conductor; the far arm behind a junction is a different conductor and keeps
                // its own colour.
                FlowState existing = SimulateRunScratchUtility.GetCellFlow(runScratch, cellIdx);
                if (existing != FlowState.Empty && existing != crawlFlow)
                {
                    FloodRevealedCells(runScratch, graphState, graphState.CellSegment[cellIdx], FlowState.Unstable);

                    // Past the meeting point this front IS the fault, so the rest of the path
                    // continues in Unstable rather than reverting to the colour that arrived.
                    crawlFlow = FlowState.Unstable;
                }

                SimulateRunScratchUtility.SetCellFlow(runScratch, cellIdx, crawlFlow);
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

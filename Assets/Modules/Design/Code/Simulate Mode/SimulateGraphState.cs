using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Text;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// One "crucial" node in the evaluation graph: an Input, Output, Gate (above/below), P-N
    /// transition, or dangling-end cell. All identity is integer — CellIndex (into GridStackState)
    /// and CrucialIndex (this node's slot in SimulateGraphState.CrucialNodes).
    ///
    /// EvalDepth is the BFS layer at which this node gets its flow value computed. Because Pass 2
    /// gives every dequeued node a clean visited set, it comes out as the exact hop distance from
    /// the Input set. Gate postponement can bump a node's depth higher after initial discovery —
    /// see Pass 2 of SimulateGraphUtility.Build.
    /// </summary>
    public struct CrucialNode
    {
        public int CellIndex;
        public GridCoord Coord;
        public int EvalDepth;
    }

    /// <summary>
    /// One "crucial" edge: a path from one crucial node to another, routed through zero or more
    /// non-crucial intermediate cells. The intermediate path is stored as a slice
    /// (PathStart, PathLength) into SimulateGraphState.PathPool — no per-edge list allocation.
    ///
    /// EvalDepth == CrucialNodes[OriginIndex].EvalDepth at the time of emit. Because gate
    /// postponement re-queues nodes at a later depth, edges enter the accumulator out of depth
    /// order; Pass 3 bucket-sorts them into OrderedEdges so the depth-step propagation walker
    /// sees them in increasing depth order.
    ///
    /// CycleDetected marks an edge into a gate whose dependency never resolved — set by the
    /// DisallowAdditionalDep branch of TryEmitEdge. The propagation loop treats such an edge as
    /// permanently unstable.
    /// </summary>
    public struct CrucialEdge
    {
        public int OriginIndex;
        public int OtherIndex;
        public int EvalDepth;
        public int PathStart;
        public int PathLength;
        public bool CycleDetected;
    }

    /// <summary>
    /// Durable output of SimulateGraphUtility.Build. Built once on Simulate-mode entry and reused
    /// across every test run in the session. Arrays are pooled — Clear zeros the counts + IsBuilt
    /// flag but keeps the array references, so subsequent builds reuse the same memory.
    /// </summary>
    public class SimulateGraphState : SharedStateComponent, IRegistrationCallbacks
    {
        // Tick in the inspector to have Build dump the node + edge table to the console.
        public bool LogGraphOnBuild;

        [NonSerialized] public bool IsBuilt;

        // Node table: 0..NodeCount-1 are valid. Array is sized to an upper bound (cellCount) and
        // not resized until a build hits a larger grid, at which point Build grows it.
        [NonSerialized] public CrucialNode[] CrucialNodes;
        [NonSerialized] public int NodeCount;

        // Edge table, depth-sorted. 0..EdgeCount-1 are valid. All crucial edges are written here
        // AFTER the bucket-sort in Pass 3 — never appended directly during BFS.
        [NonSerialized] public CrucialEdge[] OrderedEdges;
        [NonSerialized] public int EdgeCount;

        // Largest EvalDepth across OrderedEdges. Used by SimulateModeSystem.ProcessPropagating
        // to decide when the propagation walk has finished (CurrentDepth > MaxDepth).
        [NonSerialized] public int MaxDepth;

        // Shared path pool: every edge.PathStart/PathLength slices into this flat int[] of
        // cellIndices. Durable (not scratch) because DepthStepSystem reads from it every
        // frame during Propagating.
        [NonSerialized] public int[] PathPool;
        [NonSerialized] public int PathPoolUsed;

        // cellIndex → crucialIndex reverse lookup. Entries of -1 mean "this cell is not a crucial
        // node." Sized to cellCount and reset per build.
        [NonSerialized] public int[] CellToCrucial;

        // ---- Electrical segments (Pass 6) ----
        //
        // A segment is a maximal connected run of participating cells that never crosses a junction
        // (see IsSwitchableJunction) — one conductor at one potential. It is the unit flow is
        // stored and painted on: SimulateRunScratch.SegmentFlow is indexed by segmentId, so every
        // cell in a segment necessarily shows the same value.
        //
        // The metal above a gate is never joined to the transistor below it — DrawGate leaves that
        // vertical edge disconnected. Vias do connect, so a via correctly keeps both of its cells
        // inside one segment.
        //
        // Segments are built once per Build and never rebuilt mid-test, so the partition has to
        // anticipate what a gate can do rather than describe the grid as drawn. A gate-controlled
        // transistor is therefore cut from its transistor neighbours even when they currently share
        // a polarity: the moment the gate inverts it, that adjacency IS a junction, and a segment
        // spanning it would carry flow straight through the block. The reverse case needs nothing —
        // a gate that OPENS a channel shows up as flow crossing the junction edge and both segments
        // settling on the same value.

        // cellIndex → segmentId, or -1 for cells that participate in nothing.
        [NonSerialized] public int[] CellSegment;
        [NonSerialized] public int SegmentCount;

        // Segment membership in CSR form: segment s owns SegmentCells[SegmentCellStart[s] ..
        // SegmentCellStart[s + 1]), so repainting a whole segment is one contiguous walk.
        // SegmentCellStart is sized SegmentCount + 1; the last entry is the total cell count.
        [NonSerialized] public int[] SegmentCells;
        [NonSerialized] public int[] SegmentCellStart;

        // crucialIndex → segmentId. Equal to CellSegment[CrucialNodes[i].CellIndex]; kept as its
        // own table so the propagation hot loop doesn't re-derive it per edge endpoint.
        [NonSerialized] public int[] CrucialSegment;

        // Per-depth edge range table. DepthEdgeStart[d] is the first index in OrderedEdges whose
        // EvalDepth == d; DepthEdgeStart[MaxDepth + 1] == EdgeCount (sentinel). Lets
        // DepthStepSystem iterate exactly the edges at CurrentDepth without scanning the full
        // edge list. Populated by Pass 3 as a byproduct of the bucket-sort prefix-sum.
        [NonSerialized] public int[] DepthEdgeStart;

        public void OnRegister()
        {
            // Arrays stay null until first Build. Clear is also safe to call before first Build
            // (it short-circuits when arrays are null).
        }

        public void OnDeregister()
        {
        }
    }

    /// <summary>
    /// Static helpers for constructing and clearing the cached evaluation graph.
    ///
    /// Build runs eagerly once per Simulate-mode entry (kicked by ModeTransitionSystem) and is
    /// the sole writer to both SimulateGraphState and SimulateGraphBuildScratch. The build is
    /// split into five passes — see PASS comments inline. Every pass is designed to be
    /// allocation-free after first-run warmup.
    /// </summary>
    public static class SimulateGraphUtility
    {
        // Initial capacity heuristics. Grids with more cells or more complex routing may grow
        // these at runtime — each growth emits a Debug.LogWarning so we can tune capacities
        // once representative levels exist.
        //
        // Sized for the per-origin sweep in Pass 2: a region carrying k crucial nodes produces
        // up to k(k-1)/2 edges rather than k-1, and each of those carries a path slice that can
        // span the region.
        private const int InitialEdgesPerCell = 8;
        private const int InitialPathEntriesPerCell = 16;

        // Invalidates the cached graph. Called by ModeTransitionSystem on Simulate-mode exit.
        // Does NOT touch the arrays — keeping them alive is the whole reason this system is
        // allocation-free after the first build.
        public static void Clear(SimulateGraphState graphState)
        {
            graphState.IsBuilt = false;
            graphState.NodeCount = 0;
            graphState.EdgeCount = 0;
            graphState.MaxDepth = 0;
            graphState.PathPoolUsed = 0;
            graphState.SegmentCount = 0;
        }

        /// <summary>
        /// Produces the durable evaluation graph from the current grid layout. Callers should
        /// wrap this in a permission-declared ReadWriteShared context for both state components.
        ///
        /// Pass 0: ensure scratch + output capacities are adequate for cellCount.
        /// Pass 1: sweep the grid, identify crucial cells, build per-cell adjacency.
        /// Pass 2: BFS from Inputs; for each crucial-to-crucial hop, DFS through intermediate
        ///         cells to record the path. Handle gate-dependency postponement mid-BFS.
        /// Pass 3: bucket-sort discovered edges by EvalDepth into graphState.OrderedEdges.
        /// Pass 6: partition every participating cell into electrical segments.
        /// Pass 5: finalize IsBuilt + MaxDepth.
        /// </summary>
        public static void Build(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState)
        {
            Dimensions dims = gridStackState.GridStack.LayerDims;
            int numLayers = gridStackState.GridStack.GridLayers.Length;
            int numCols = dims.X;
            int numRows = dims.Y;
            int cellsPerLayer = numCols * numRows;
            int cellCount = numLayers * cellsPerLayer;

            Pass0_EnsureCapacityAndReset(graphState, scratch, cellCount);
            Pass1_DiscoverNodesAndAdjacency(graphState, scratch, gridStackState, numLayers, numCols, numRows, cellsPerLayer);
            Pass2_BfsAndPathDiscovery(graphState, scratch, gridStackState, numCols, cellsPerLayer);
            int maxDepth = Pass3_BucketSortEdgesByDepth(graphState, scratch);
            Pass6_PartitionSegments(graphState, scratch, gridStackState, numCols, cellsPerLayer, cellCount);
            Pass5_Finalize(graphState, maxDepth);

            if (graphState.LogGraphOnBuild)
            {
                LogGraph(graphState, gridStackState, numCols, cellsPerLayer);
            }
        }

        // Dumps the built graph to the console. Each edge prints as origin → dest with the depth
        // it will be evaluated at, so a cell that never lights can be traced to either a missing
        // edge or an edge whose origin never carried flow.
        private static void LogGraph(SimulateGraphState graphState, GridStackState gridStackState, int numCols, int cellsPerLayer)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[SimulateGraphUtility] nodes=").Append(graphState.NodeCount)
                .Append(" edges=").Append(graphState.EdgeCount)
                .Append(" segments=").Append(graphState.SegmentCount)
                .Append(" maxDepth=").Append(graphState.MaxDepth)
                .Append(" pathPoolUsed=").Append(graphState.PathPoolUsed);

            for (int e = 0; e < graphState.EdgeCount; e++)
            {
                CrucialEdge edge = graphState.OrderedEdges[e];
                sb.AppendLine();
                AppendNodeLabel(sb, graphState, gridStackState, edge.OriginIndex);
                sb.Append(" -> ");
                AppendNodeLabel(sb, graphState, gridStackState, edge.OtherIndex);
                sb.Append("  depth=").Append(edge.EvalDepth)
                    .Append(" pathLen=").Append(edge.PathLength)
                    .Append(" seg ").Append(graphState.CrucialSegment[edge.OriginIndex])
                    .Append("->").Append(graphState.CrucialSegment[edge.OtherIndex]);
                if (edge.CycleDetected) { sb.Append(" CYCLE"); }
            }

            // Segment membership. Cells sharing an id are one conductor and always show one value,
            // so this is where to confirm a merge region came out as a single segment.
            for (int s = 0; s < graphState.SegmentCount; s++)
            {
                sb.AppendLine();
                sb.Append("segment ").Append(s).Append(": ");
                int start = graphState.SegmentCellStart[s];
                int end = graphState.SegmentCellStart[s + 1];
                for (int i = start; i < end; i++)
                {
                    if (i > start) { sb.Append(' '); }
                    AppendCellLabel(sb, gridStackState, graphState.SegmentCells[i], numCols, cellsPerLayer);
                }
            }

            Debug.Log(sb.ToString());
        }

        // "L0C3R1(Metal/Via)" — enough to find the cell on the grid and tell why it's crucial.
        private static void AppendNodeLabel(StringBuilder sb, SimulateGraphState graphState, GridStackState gridStackState, int crucialIdx)
        {
            GridCoord coord = graphState.CrucialNodes[crucialIdx].Coord;
            GridCell cell = GridStackUtility.GetCellDirect(gridStackState, coord);
            AppendCoordLabel(sb, cell, coord.Layer, coord.Col, coord.Row);
        }

        // Same label from a flat cellIndex, for the segment listing.
        private static void AppendCellLabel(StringBuilder sb, GridStackState gridStackState, int cellIdx, int numCols, int cellsPerLayer)
        {
            int layer = cellIdx / cellsPerLayer;
            int rem = cellIdx - layer * cellsPerLayer;
            int row = rem / numCols;
            int col = rem - row * numCols;
            AppendCoordLabel(sb, GridStackUtility.GetCellDirect(gridStackState, layer, col, row), layer, col, row);
        }

        private static void AppendCoordLabel(StringBuilder sb, GridCell cell, int layer, int col, int row)
        {
            sb.Append('L').Append(layer)
                .Append('C').Append(col)
                .Append('R').Append(row)
                .Append('(').Append(cell.CellType).Append('/').Append(cell.TransferType).Append(')');
        }

        #region Pass 0

        // ====================================================================================
        // PASS 0 — Capacity hint + state reset
        // ------------------------------------------------------------------------------------
        // Lazy-allocate every array on first run; on subsequent runs resize only if the
        // grid has grown. Growth is loud (Debug.LogWarning) so undersized initial
        // heuristics become visible during playtesting.
        //
        // Resets all counts + stamp-invalidates visited marks. The stamp-reset trick avoids
        // clearing VisitStamps with an O(cellCount) loop — we bump the stamp and any prior marks
        // become stale. The no-return relation is a flat pair buffer, reset by zeroing its count.
        // CellToCrucial must still be cleared to -1 because its sentinel is relied on downstream.
        // ====================================================================================
        private static void Pass0_EnsureCapacityAndReset(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int cellCount)
        {
            EnsureCapacity(ref graphState.CellToCrucial, cellCount, nameof(graphState.CellToCrucial));
            EnsureCapacity(ref graphState.CellSegment, cellCount, nameof(graphState.CellSegment));
            EnsureCapacity(ref graphState.SegmentCells, cellCount, nameof(graphState.SegmentCells));
            EnsureCapacity(ref graphState.SegmentCellStart, cellCount + 1, nameof(graphState.SegmentCellStart));
            EnsureCapacity(ref graphState.CrucialSegment, cellCount, nameof(graphState.CrucialSegment));
            EnsureCapacity(ref graphState.CrucialNodes, cellCount, nameof(graphState.CrucialNodes));
            EnsureCapacity(ref graphState.OrderedEdges, cellCount * InitialEdgesPerCell, nameof(graphState.OrderedEdges));
            EnsureCapacity(ref graphState.PathPool, cellCount * InitialPathEntriesPerCell, nameof(graphState.PathPool));

            EnsureCapacity(ref scratch.CellAdjStart, cellCount, nameof(scratch.CellAdjStart));
            EnsureCapacity(ref scratch.CellAdjCount, cellCount, nameof(scratch.CellAdjCount));
            EnsureCapacity(ref scratch.CellAdjDest, cellCount * 6, nameof(scratch.CellAdjDest));
            EnsureCapacity(ref scratch.WorkQueue, cellCount, nameof(scratch.WorkQueue));
            EnsureCapacity(ref scratch.VisitStamps, cellCount, nameof(scratch.VisitStamps));
            EnsureCapacity(ref scratch.NoReturnPairs, cellCount * InitialEdgesPerCell * 2, nameof(scratch.NoReturnPairs));
            EnsureCapacity(ref scratch.DfsPathBuffer, cellCount, nameof(scratch.DfsPathBuffer));
            EnsureCapacity(ref scratch.DfsReachedCrucial, cellCount, nameof(scratch.DfsReachedCrucial));
            EnsureCapacity(ref scratch.DfsReachedPathStart, cellCount, nameof(scratch.DfsReachedPathStart));
            EnsureCapacity(ref scratch.DfsReachedPathLength, cellCount, nameof(scratch.DfsReachedPathLength));
            EnsureCapacity(ref scratch.PostponedPairs, cellCount * 2, nameof(scratch.PostponedPairs));
            EnsureCapacity(ref scratch.AwaitingDependency, cellCount, nameof(scratch.AwaitingDependency));
            EnsureCapacity(ref scratch.EvaluatedForDependency, cellCount, nameof(scratch.EvaluatedForDependency));
            EnsureCapacity(ref scratch.DisallowAdditionalDep, cellCount, nameof(scratch.DisallowAdditionalDep));
            EnsureCapacity(ref scratch.Processed, cellCount, nameof(scratch.Processed));
            EnsureCapacity(ref scratch.UnsortedEdges, cellCount * InitialEdgesPerCell, nameof(scratch.UnsortedEdges));
            EnsureCapacity(ref scratch.SegmentCursor, cellCount + 1, nameof(scratch.SegmentCursor));

            graphState.NodeCount = 0;
            graphState.EdgeCount = 0;
            graphState.MaxDepth = 0;
            graphState.PathPoolUsed = 0;
            graphState.SegmentCount = 0;

            scratch.CellAdjDestUsed = 0;
            scratch.WorkHead = 0;
            scratch.WorkTail = 0;
            scratch.DfsPathDepth = 0;
            scratch.DfsReachedCrucialCount = 0;
            scratch.PostponedCount = 0;
            scratch.UnsortedEdgeCount = 0;
            scratch.NoReturnPairCount = 0;

            // Bump the visit stamp: a single int increment invalidates every prior "visited" mark
            // in O(1). Much cheaper than walking cellCount entries to clear them to zero.
            scratch.CurrentVisitStamp++;

            // Clear CellToCrucial so cells without crucial assignments show -1. This IS an O(cellCount)
            // pass, but -1 is the sentinel we rely on everywhere downstream — cannot use a stamp.
            // CellSegment shares the sentinel and the loop; Pass 6 also treats -1 as "not yet
            // assigned to a segment", so it doubles as that flood fill's visited marker.
            for (int i = 0; i < cellCount; i++)
            {
                graphState.CellSegment[i] = -1;
                graphState.CellToCrucial[i] = -1;
            }

            // The dependency bool arrays are indexed by crucialIndex (not cellIndex), and crucial
            // indices only start getting assigned in Pass 1. Since we don't know in advance how
            // many crucial nodes we'll have, clearing up to cellCount (the upper bound) is safe
            // and keeps Pass 2 branches trivial.
            Array.Clear(scratch.AwaitingDependency, 0, cellCount);
            Array.Clear(scratch.EvaluatedForDependency, 0, cellCount);
            Array.Clear(scratch.DisallowAdditionalDep, 0, cellCount);
            Array.Clear(scratch.Processed, 0, cellCount);
        }

        #endregion // Pass 0

        #region Pass 1

        // ====================================================================================
        // PASS 1 — Discover crucial nodes + build per-cell adjacency
        // ------------------------------------------------------------------------------------
        // One linear sweep over all (layer, row, col) positions. Two things happen per cell:
        //
        //   (a) Classification. Is this cell a crucial node? A crucial node is any Input,
        //       Output, Gate (above or below), P-N transistor transition, or dangling-end
        //       path (only one cardinal connection). If so, append to CrucialNodes and
        //       mark CellToCrucial[cellIndex]. Inputs additionally get seeded into the
        //       BFS work queue with EvalDepth = 0 — they are the start nodes for Pass 2.
        //
        //   (b) Adjacency. For each of the 6 edge directions, if the cell has a Connected
        //       edge AND the neighbor is in-bounds AND the neighbor cell is non-empty,
        //       record that adjacency in the flat CellAdjDest buffer. Adjacency is tracked
        //       for EVERY non-empty cell (not only crucial ones) because Pass 2 walks
        //       through intermediate non-crucial cells to discover crucial-to-crucial paths.
        //
        // The two-pass structure used by the prototype (separate node-discovery and edge-
        // construction passes) is collapsed into a single sweep here. Prototype's two-pass
        // shape existed to match its data-structure layout; since we don't use dictionaries
        // we can classify and record adjacency at the same time.
        //
        // Note on GateAbove-only cells: the prototype explicitly checks for cells with
        // CellType.NONE that still carry TransferType.GateAbove. These participate as gate
        // controllers even though they have no electrical type. We mirror that here.
        // ====================================================================================
        private static void Pass1_DiscoverNodesAndAdjacency(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState, int numLayers, int numCols, int numRows, int cellsPerLayer)
        {
            for (int layer = 0; layer < numLayers; layer++)
            {
                for (int row = 0; row < numRows; row++)
                {
                    for (int col = 0; col < numCols; col++)
                    {
                        GridCell cell = GridStackUtility.GetCellDirect(gridStackState, layer, col, row);

                        // Skip fully empty cells. Exception: a cell marked GateAbove transfer-type
                        // still participates even with CellType.NONE.
                        bool cellParticipates = cell.CellType != CellType.NONE
                            || cell.TransferType == TransferType.GateAbove;
                        if (!cellParticipates) { continue; }

                        int cellIndex = CellIndex(layer, col, row, numCols, cellsPerLayer);

                        // --- (a) Crucial classification ---------------------------------------
                        //
                        // Order matters only for performance: put cheap boolean checks before
                        // the more expensive IsTransistorTransition neighbor scan and the
                        // condensed-edge dangling-end check.

                        bool isCrucial =
                            cell.CellType == CellType.Input
                            || cell.CellType == CellType.Output
                            || cell.TransferType == TransferType.GateAbove
                            || cell.TransferType == TransferType.GateBelow;

                        if (!isCrucial)
                        {
                            isCrucial |= IsTransistorTransition(gridStackState, layer, col, row, cell, numLayers, numCols, numRows);
                        }

                        if (!isCrucial)
                        {
                            // Dangling-end check: condense the 6-direction edge array down to the
                            // 4 cardinal directions and count connections. Exactly one connection
                            // means the cell is a path terminus that nothing branches from — it
                            // should be a crucial endpoint so propagation can settle there.
                            EdgeState[] condensed = EdgeUtility.CondenseEdges(cell.Edges);
                            isCrucial = EdgeUtility.NumConnections(condensed) == 1;
                        }

                        if (isCrucial)
                        {
                            int crucialIdx = graphState.NodeCount;
                            graphState.CrucialNodes[crucialIdx] = new CrucialNode()
                            {
                                CellIndex = cellIndex,
                                Coord = new GridCoord(layer, col, row),
                                EvalDepth = 0,
                            };
                            graphState.CellToCrucial[cellIndex] = crucialIdx;
                            graphState.NodeCount++;

                            // Inputs are the BFS seed set. Everything else is reached via BFS
                            // and gets its real EvalDepth assigned at discovery time.
                            if (cell.CellType == CellType.Input)
                            {
                                PushWork(scratch, crucialIdx);
                            }
                        }

                        // --- (b) Adjacency recording ------------------------------------------
                        //
                        // Start index for this cell's neighbors in the shared CellAdjDest buffer.
                        // CellAdjCount is written incrementally as we find valid edges.

                        scratch.CellAdjStart[cellIndex] = scratch.CellAdjDestUsed;
                        int neighborsFound = 0;

                        for (int dir = 0; dir < 6; dir++)
                        {
                            if (cell.Edges[dir].EdgeState != EdgeState.Connected) { continue; }

                            GridStackUtility.GetOffsetOfDir((EdgeDir)dir, out Vector2Int gridOffset, out int layerOffset);
                            int adjLayer = layer + layerOffset;
                            int adjCol = col + gridOffset.x;
                            int adjRow = row + gridOffset.y;

                            // Bounds check. Layer-wise: [0, numLayers). Column/row: [0, dim).
                            if (adjLayer < 0 || adjLayer >= numLayers) { continue; }
                            if (adjCol < 0 || adjCol >= numCols) { continue; }
                            if (adjRow < 0 || adjRow >= numRows) { continue; }

                            // Reject neighbors that have neither a CellType nor a GateAbove
                            // transfer — same "does this cell participate" rule as classification.
                            GridCell adjCell = GridStackUtility.GetCellDirect(gridStackState, adjLayer, adjCol, adjRow);
                            bool adjParticipates = adjCell.CellType != CellType.NONE
                                || adjCell.TransferType == TransferType.GateAbove;
                            if (!adjParticipates) { continue; }

                            int adjCellIndex = CellIndex(adjLayer, adjCol, adjRow, numCols, cellsPerLayer);
                            scratch.CellAdjDest[scratch.CellAdjDestUsed++] = adjCellIndex;
                            neighborsFound++;
                        }

                        scratch.CellAdjCount[cellIndex] = neighborsFound;
                    }
                }
            }
        }

        #endregion // Pass 1

        #region Pass 2

        // ====================================================================================
        // PASS 2 — BFS crucial-to-crucial + DFS path discovery
        // ------------------------------------------------------------------------------------
        // The prototype's SetAllNodesAllPaths algorithm, ported to indices + preallocated
        // buffers. Shape:
        //
        //   outer BFS loop:
        //     dequeue currCrucialIdx from WorkQueue
        //     bump CurrentVisitStamp so this origin sweeps a clean visited set
        //     for each outgoing neighbor of currCellIdx:
        //        DFS through non-crucial cells to find reachable crucial nodes
        //        for each reached crucial: emit a CrucialEdge (with path slice copied into
        //            PathPool) unless the reciprocal edge already exists (NoReturn) or
        //            gate-dependency rules defer it
        //
        // Visited is scoped to one origin's sweep (CurrentVisitStamp is bumped per dequeue), so
        // every crucial node reaches every crucial node it can reach through non-crucial cells,
        // regardless of who else was processed at the same depth. That makes discovery
        // order-independent and makes EvalDepth exactly the hop distance from the Input set:
        // each node dequeued at depth d assigns d+1 to all of its crucial neighbours.
        //
        // Reciprocal suppression (NoReturn): once an edge O→C is recorded we must never record
        // C→O — a cumulative pair buffer (see SimulateGraphBuildScratch.NoReturnPairs). A ban
        // never blocks a FIRST reach (it only exists once the reverse edge was recorded, which
        // means the target already holds a depth), so it cannot delay a depth assignment.
        // Processed stops re-enqueuing nodes, so the work queue strictly drains.
        //
        // The key subtlety is the gate-dependency mechanic. A GateBelow (underside of a gate)
        // can only be correctly evaluated AFTER its matching GateAbove (metal connector) has
        // been evaluated, because the above-signal might invert the below-node's type. So
        // when DFS reaches a GateBelow whose above-node is not yet "EvaluatedForDependency",
        // we DON'T emit that edge — we stash the pair in PostponedPairs and continue.
        //
        // When a GateAbove is later reached, we mark it Evaluated and re-enqueue any postponed
        // dependents at EvalDepth = currDepth + 1 (so their evaluation happens one layer
        // deeper than the gate's). We also reset the corresponding below-cell's VisitStamp so
        // it becomes revisitable on the next depth layer.
        //
        // When BFS runs dry but postponements remain, those dependencies will never resolve —
        // we apply the prototype's "progress past them" fallback: re-enqueue dependents at
        // currDepth + 1 and mark the blocking below-nodes DisallowAdditionalDep (which causes
        // future edges to those nodes to be flagged CycleDetected via Pass 4).
        //
        // Edges produced here are appended to scratch.UnsortedEdges (not directly written to
        // graphState.OrderedEdges) because gate postponement can emit edges out of EvalDepth
        // order. Pass 3 bucket-sorts them.
        // ====================================================================================
        private static void Pass2_BfsAndPathDiscovery(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState, int numCols, int cellsPerLayer)
        {
            int currDepth = 0;

            while (scratch.WorkHead < scratch.WorkTail || scratch.PostponedCount > 0)
            {
                // --- Drain the postponed queue if BFS is out of fresh work --------------------
                //
                // Guarantees forward progress when a dependency will never be satisfied (e.g. a
                // disconnected gate). Mirrors prototype lines 1230–1259.
                if (scratch.WorkHead == scratch.WorkTail)
                {
                    FlushUnresolvedPostponements(graphState, scratch, currDepth, numCols, cellsPerLayer);
                    // After flushing, the work queue has fresh entries but may cycle back here
                    // immediately if the flush didn't actually add anything (shouldn't happen in
                    // practice; the flush always adds if PostponedCount > 0).
                    continue;
                }

                int currCrucialIdx = scratch.WorkQueue[scratch.WorkHead++];
                int currCellIdx = graphState.CrucialNodes[currCrucialIdx].CellIndex;
                int nodeDepth = graphState.CrucialNodes[currCrucialIdx].EvalDepth;

                // Mark this crucial node processed. Future DFS reaches still RECORD an edge into
                // it (so a second driver's flow reaches it during propagation) but EnqueueIfUnprocessed
                // won't re-enqueue it — that's what bounds the BFS on mutually-cell-reachable crucials.
                scratch.Processed[currCrucialIdx] = true;

                // Give this origin a clean visited set. Visited is scoped to ONE crucial node's
                // sweep, not to a depth layer — sharing it across a layer lets whichever node is
                // dequeued first claim a shared region, leaving every other node on that region
                // unable to emit an edge into it. The stamp bump is O(1).
                scratch.CurrentVisitStamp++;
                currDepth = nodeDepth;

                int adjStart = scratch.CellAdjStart[currCellIdx];
                int adjEnd = adjStart + scratch.CellAdjCount[currCellIdx];

                // One DFS per outgoing neighbor. Each DFS produces a set of reached crucial
                // nodes + a snapshot path.
                for (int nbIdx = adjStart; nbIdx < adjEnd; nbIdx++)
                {
                    int neighborCellIdx = scratch.CellAdjDest[nbIdx];

                    // Reset DFS workspace.
                    scratch.DfsPathDepth = 0;
                    scratch.DfsReachedCrucialCount = 0;
                    scratch.DfsPathBuffer[scratch.DfsPathDepth++] = currCellIdx;

                    // Run the iterative DFS.
                    DfsFromNeighbor(graphState, scratch, gridStackState, currCrucialIdx, currCellIdx, neighborCellIdx, currDepth);

                    // Emit one CrucialEdge per reached crucial node. Gate-dependency logic lives
                    // here because it affects whether the edge is emitted now or deferred. The
                    // path slice for each reached crucial was captured by TryRecordReachedCrucial
                    // when DFS was at maximum depth — pass it in so TryEmitEdge doesn't re-snapshot
                    // from a buffer that's already been popped.
                    for (int r = 0; r < scratch.DfsReachedCrucialCount; r++)
                    {
                        int reachedCrucialIdx = scratch.DfsReachedCrucial[r];
                        int pathStart = scratch.DfsReachedPathStart[r];
                        int pathLength = scratch.DfsReachedPathLength[r];
                        TryEmitEdge(graphState, scratch, gridStackState, currCrucialIdx, reachedCrucialIdx,
                            pathStart, pathLength, currDepth, numCols, cellsPerLayer);
                    }
                }
            }
        }

        #endregion // Pass 2

        #region Pass 3

        // ====================================================================================
        // PASS 3 — Bucket-sort edges by depth
        // ------------------------------------------------------------------------------------
        // Why this pass exists: gate postponement in Pass 2 can emit an edge with
        // EvalDepth == currDepth + 1 AFTER we've already emitted edges at later depths
        // from other BFS branches. The UnsortedEdges buffer is not monotonic in EvalDepth.
        //
        // DepthStepSystem walks OrderedEdges one depth layer at a time, so monotonic
        // ordering is a correctness requirement — not just an optimization.
        //
        // Returns maxDepth so Pass 5 can stash it on graphState.
        // ====================================================================================
        private static int Pass3_BucketSortEdgesByDepth(SimulateGraphState graphState, SimulateGraphBuildScratch scratch)
        {
            int maxDepth = 0;
            for (int i = 0; i < scratch.UnsortedEdgeCount; i++)
            {
                int d = scratch.UnsortedEdges[i].EvalDepth;
                if (d > maxDepth) { maxDepth = d; }
            }

            BucketSortEdgesByDepth(graphState, scratch, maxDepth);
            return maxDepth;
        }

        #endregion // Pass 3

        // ====================================================================================
        // NOTE — there is no cycle-detection pass.
        // ------------------------------------------------------------------------------------
        // A back-edge DFS over OrderedEdges used to live here. It could not survive two facts:
        //
        //   1. Pass 2 sweeps per origin, so every edge runs from an earlier-dequeued node to a
        //      later-dequeued one (had the target been dequeued first, its own complete sweep
        //      would have recorded the reverse and banned this direction). That is a topological
        //      order, so back-edges effectively never occurred and the pass was dead code.
        //   2. TryRecordReachedCrucial now emits P↔N junctions in BOTH directions, which reads
        //      as a 2-cycle on every transistor on the board — the pass would have driven the
        //      whole grid Unstable.
        //
        // No replacement is needed. A DRIVEN feedback loop pushes a second, conflicting value
        // into its own segment, and AssignSegmentFlow's absorbing Unstable reports it. An
        // UNDRIVEN feedback ring stays Empty and its output simply mismatches, which is an
        // Incorrect row rather than an Unstable one — the right answer, since nothing about it
        // is electrically unstable. An unstable region that reaches no output leaves the verdict
        // alone because ProcessResolvingTest scores the output segments, not a global flag.
        //
        // CrucialEdge.CycleDetected survives and is still set by the DisallowAdditionalDep path
        // in TryEmitEdge — a build-time signal that a gate dependency never resolved, which is
        // unrelated to the deleted DFS.
        // ====================================================================================

        #region Pass 5

        // ====================================================================================
        // PASS 5 — Finalize
        // ====================================================================================
        private static void Pass5_Finalize(SimulateGraphState graphState, int maxDepth)
        {
            graphState.MaxDepth = maxDepth;
            graphState.IsBuilt = true;
        }

        #endregion // Pass 5

        #region Pass 6

        // ====================================================================================
        // PASS 6 — Partition every participating cell into electrical segments
        // ------------------------------------------------------------------------------------
        // A segment is a maximal connected run of participating cells that never crosses a
        // junction — one conductor at one potential. Flow is stored per segment and painted per
        // segment, so this partition is what guarantees a wire can never display two colours at
        // once and what makes a late conflicting driver recolour the whole region.
        //
        // Flood fill over the same cell adjacency Pass 1 built (CellAdjStart/Count/Dest, which
        // covers EVERY participating cell), refusing hops where IsSwitchableJunction is true —
        // physical P-N pairs, plus same-type pairs under a gate, whose effective polarity can flip
        // mid-run. A conductor that a gate can split has to be partitioned as though it were
        // already split, since segment flow is instantaneous and would otherwise carry straight
        // through the cell the gate just inverted. Two conductors joined by a channel the gate
        // OPENS need no special handling — flow crosses the junction edge and both settle on the
        // same value.
        //
        // Sweeps all cellCount cells rather than seeding from crucial nodes, so a region holding
        // no crucial node at all (a closed metal ring) still gets an id and stays uniform.
        // CellSegment was pre-cleared to -1 in Pass 0 and doubles as the visited marker, so the
        // fill needs no visit stamps.
        //
        // Membership is then grouped into CSR form (SegmentCells / SegmentCellStart) with the same
        // counting-sort shape as BucketSortEdgesByDepth, so repainting one segment at run time is
        // a contiguous walk rather than a scan of the grid.
        // ====================================================================================
        private static void Pass6_PartitionSegments(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState, int numCols, int cellsPerLayer, int cellCount)
        {
            graphState.SegmentCount = 0;

            for (int seed = 0; seed < cellCount; seed++)
            {
                if (graphState.CellSegment[seed] >= 0) { continue; }
                if (!CellParticipates(gridStackState, seed, numCols, cellsPerLayer)) { continue; }

                int segmentId = graphState.SegmentCount++;
                graphState.CellSegment[seed] = segmentId;

                scratch.WorkHead = 0;
                scratch.WorkTail = 0;
                PushWork(scratch, seed);

                while (scratch.WorkHead < scratch.WorkTail)
                {
                    int cellIdx = scratch.WorkQueue[scratch.WorkHead++];
                    GridCell cell = GetCellByIndex(gridStackState, cellIdx, numCols, cellsPerLayer);

                    int adjStart = scratch.CellAdjStart[cellIdx];
                    int adjEnd = adjStart + scratch.CellAdjCount[cellIdx];
                    for (int a = adjStart; a < adjEnd; a++)
                    {
                        int nbrIdx = scratch.CellAdjDest[a];
                        if (graphState.CellSegment[nbrIdx] >= 0) { continue; }

                        GridCell nbrCell = GetCellByIndex(gridStackState, nbrIdx, numCols, cellsPerLayer);

                        // A junction separates two conductors that can hold different values —
                        // including a same-type pair under a gate, which becomes a real junction
                        // the moment the gate inverts one side.
                        if (IsSwitchableJunction(cell, nbrCell)) { continue; }

                        graphState.CellSegment[nbrIdx] = segmentId;
                        PushWork(scratch, nbrIdx);
                    }
                }
            }

            GroupCellsBySegment(graphState, scratch, cellCount);

            for (int c = 0; c < graphState.NodeCount; c++)
            {
                graphState.CrucialSegment[c] = graphState.CellSegment[graphState.CrucialNodes[c].CellIndex];
            }
        }

        // Counting-sort cell indices into SegmentCells grouped by segment, leaving each segment's
        // start offset in SegmentCellStart[s] and the total in SegmentCellStart[SegmentCount].
        private static void GroupCellsBySegment(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int cellCount)
        {
            int bucketCount = graphState.SegmentCount;

            // Histogram into [s + 1] so the prefix sum below leaves starts in place without a
            // separate offset step. Non-participating cells (segment -1) are skipped throughout.
            for (int s = 0; s <= bucketCount; s++) { graphState.SegmentCellStart[s] = 0; }
            for (int i = 0; i < cellCount; i++)
            {
                int s = graphState.CellSegment[i];
                if (s < 0) { continue; }
                graphState.SegmentCellStart[s + 1]++;
            }
            for (int s = 0; s < bucketCount; s++)
            {
                graphState.SegmentCellStart[s + 1] += graphState.SegmentCellStart[s];
            }

            // Placement uses its own write cursors so SegmentCellStart keeps the start offsets
            // the run-time paint walk reads.
            for (int s = 0; s < bucketCount; s++) { scratch.SegmentCursor[s] = graphState.SegmentCellStart[s]; }
            for (int i = 0; i < cellCount; i++)
            {
                int s = graphState.CellSegment[i];
                if (s < 0) { continue; }
                graphState.SegmentCells[scratch.SegmentCursor[s]++] = i;
            }
        }

        // True if a cell takes part in the electrical graph at all. Same rule Pass 1 classifies
        // with: a cell with no CellType still participates when it carries a GateAbove transfer.
        private static bool CellParticipates(GridStackState gridStackState, int cellIdx, int numCols, int cellsPerLayer)
        {
            GridCell cell = GetCellByIndex(gridStackState, cellIdx, numCols, cellsPerLayer);
            return cell.CellType != CellType.NONE || cell.TransferType == TransferType.GateAbove;
        }

        // Decompose a flat cellIndex back to (layer, col, row) and read the cell. Inverse of
        // CellIndex; see that helper for the packing.
        private static GridCell GetCellByIndex(GridStackState gridStackState, int cellIdx, int numCols, int cellsPerLayer)
        {
            int layer = cellIdx / cellsPerLayer;
            int rem = cellIdx - layer * cellsPerLayer;
            int row = rem / numCols;
            int col = rem - row * numCols;
            return GridStackUtility.GetCellDirect(gridStackState, layer, col, row);
        }

        #endregion // Pass 6

        // ====================================================================================
        // Shared helpers used across passes follow below. They're grouped by which pass invokes
        // them; each pass method above delegates to the helpers in the matching region.
        // ====================================================================================

        #region Pass 1 helpers

        // True if this P/N transistor cell borders a junction via any connected edge — either a
        // transistor of the opposite type, or a gate-controlled transistor whose polarity can flip
        // mid-run. Both sides of a junction have to be crucial so the junction exists as an edge:
        // DepthStepSystem only applies its diode rule when both of an edge's ENDPOINTS are
        // transistors, so a junction buried in the middle of an edge's path is never evaluated.
        private static bool IsTransistorTransition(GridStackState gridStackState, int layer, int col, int row, GridCell cell, int numLayers, int numCols, int numRows)
        {
            // Metal layer can't host transistors; skip.
            if (layer == (int)StackLayer.Metal) { return false; }
            if (cell.CellType != CellType.PTransistor && cell.CellType != CellType.NTransistor) { return false; }

            for (int dir = 0; dir < 6; dir++)
            {
                if (cell.Edges[dir].EdgeState != EdgeState.Connected) { continue; }

                GridStackUtility.GetOffsetOfDir((EdgeDir)dir, out Vector2Int gridOffset, out int layerOffset);
                int adjLayer = layer + layerOffset;
                int adjCol = col + gridOffset.x;
                int adjRow = row + gridOffset.y;

                if (adjLayer < 0 || adjLayer >= numLayers) { continue; }
                if (adjCol < 0 || adjCol >= numCols) { continue; }
                if (adjRow < 0 || adjRow >= numRows) { continue; }

                GridCell adjCell = GridStackUtility.GetCellDirect(gridStackState, adjLayer, adjCol, adjRow);
                if (IsSwitchableJunction(cell, adjCell)) { return true; }
            }

            return false;
        }

        // True if a hop between two adjacent cells is a junction the simulation may have to gate.
        // Two cases, and the second is why this is not just a physical-polarity test:
        //
        //   - A physical P-N pair. A diode; the two sides are separate conductors that can carry
        //     different values.
        //   - A same-type pair where one side sits under a gate. A gate flips its cell's EFFECTIVE
        //     polarity mid-run, turning a uniform chain into P-N-P (or N-P-N), so the boundary has
        //     to mean "could ever be a junction" rather than "is one right now". Deciding this from
        //     physical polarity alone leaves the inversion with no edge and no segment boundary to
        //     act on, and the chain conducts as though the gate were not there.
        //
        // Both cells must be transistors, so a via from a gate cell up to metal is not a junction
        // and stays one conductor.
        private static bool IsSwitchableJunction(GridCell a, GridCell b)
        {
            if (!IsTransistorCell(a) || !IsTransistorCell(b)) { return false; }
            if (a.CellType != b.CellType) { return true; }
            return a.TransferType == TransferType.GateBelow || b.TransferType == TransferType.GateBelow;
        }

        private static bool IsTransistorCell(GridCell cell)
        {
            return cell.CellType == CellType.PTransistor || cell.CellType == CellType.NTransistor;
        }

        #endregion // Pass 1 helpers

        #region Pass 2 helpers

        // Iterative DFS on the cell-level adjacency graph. Starts from (origin -> firstNeighbor)
        // and walks through non-crucial cells, stopping whenever it hits a crucial cell. All
        // reached crucial cells are recorded in scratch.DfsReachedCrucial.
        //
        // The "stack" is implicit in scratch.DfsPathBuffer: pushing = DfsPathBuffer[depth++]
        // and popping = depth--. Each cell also needs a cursor indicating which neighbor to
        // explore next; we keep that in a separate scratch (allocated locally via stackalloc
        // for typical depths, since true DFS depth is bounded by cellCount).
        //
        // The origin is already pushed before this is called (caller does
        // DfsPathBuffer[0] = originCellIdx). This function pushes the first neighbor and
        // proceeds from there.
        private static unsafe void DfsFromNeighbor(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState, int originCrucialIdx, int originCellIdx, int firstNeighborCellIdx, int currDepth)
        {
            // Per-frame neighbor cursor. Bounded by path depth (<= cellCount). 256 covers every
            // plausible grid size; grow dynamically if we ever exceed.
            const int MaxDfsFrameStack = 256;
            int* nextNbCursor = stackalloc int[MaxDfsFrameStack];

            // Mark origin visited at this stamp so DFS won't re-enter it.
            scratch.VisitStamps[originCellIdx] = scratch.CurrentVisitStamp;

            // Push first neighbor onto path + initialize its cursor.
            // Check if the first neighbor is already blocked or visited before pushing.
            if (scratch.VisitStamps[firstNeighborCellIdx] == scratch.CurrentVisitStamp)
            {
                return;
            }

            scratch.DfsPathBuffer[scratch.DfsPathDepth] = firstNeighborCellIdx;
            nextNbCursor[scratch.DfsPathDepth] = 0;
            scratch.DfsPathDepth++;
            scratch.VisitStamps[firstNeighborCellIdx] = scratch.CurrentVisitStamp;

            // If the first neighbor is itself a crucial cell, record it and pop — don't
            // descend past a crucial boundary.
            int firstCrucial = graphState.CellToCrucial[firstNeighborCellIdx];
            if (firstCrucial >= 0 && firstNeighborCellIdx != originCellIdx)
            {
                TryRecordReachedCrucial(graphState, scratch, gridStackState, originCrucialIdx, firstCrucial, currDepth);
                scratch.DfsPathDepth--; // pop back to just the origin
                return;
            }

            // Standard iterative DFS on frames (cellIdx, cursorIntoAdjList).
            while (scratch.DfsPathDepth > 1)
            {
                int frameDepth = scratch.DfsPathDepth - 1;
                int currCellIdx = scratch.DfsPathBuffer[frameDepth];
                int adjStart = scratch.CellAdjStart[currCellIdx];
                int adjEnd = adjStart + scratch.CellAdjCount[currCellIdx];

                // Advance to the next unvisited neighbor.
                bool descended = false;
                while (nextNbCursor[frameDepth] < adjEnd - adjStart)
                {
                    int dest = scratch.CellAdjDest[adjStart + nextNbCursor[frameDepth]];
                    nextNbCursor[frameDepth]++;

                    // Already visited this depth layer? Skip. (Reciprocal suppression is applied
                    // at record time in TryRecordReachedCrucial, mirroring the prototype, which
                    // only blocks recording the crucial hop — never traversal of plain cells.)
                    if (scratch.VisitStamps[dest] == scratch.CurrentVisitStamp) { continue; }

                    // Mark visited.
                    scratch.VisitStamps[dest] = scratch.CurrentVisitStamp;

                    int destCrucial = graphState.CellToCrucial[dest];
                    if (destCrucial >= 0)
                    {
                        // Hit a crucial node — record the hop (origin -> destCrucial) via the
                        // current path (plus the dest cell, which we also push so the path-
                        // slice includes it) and DO NOT descend further.
                        if (scratch.DfsPathDepth < scratch.DfsPathBuffer.Length)
                        {
                            scratch.DfsPathBuffer[scratch.DfsPathDepth++] = dest;
                        }
                        TryRecordReachedCrucial(graphState, scratch, gridStackState, originCrucialIdx, destCrucial, currDepth);
                        scratch.DfsPathDepth--; // pop dest
                        continue;
                    }

                    // Non-crucial; descend.
                    if (scratch.DfsPathDepth >= scratch.DfsPathBuffer.Length)
                    {
                        Debug.LogWarning("[SimulateGraphUtility] DfsPathBuffer overflow; aborting descent");
                        break;
                    }
                    if (frameDepth + 1 >= MaxDfsFrameStack)
                    {
                        Debug.LogWarning("[SimulateGraphUtility] nextNbCursor stack overflow; aborting descent");
                        break;
                    }
                    scratch.DfsPathBuffer[scratch.DfsPathDepth] = dest;
                    nextNbCursor[scratch.DfsPathDepth] = 0;
                    scratch.DfsPathDepth++;
                    descended = true;
                    break;
                }

                if (!descended)
                {
                    // Exhausted this cell's neighbors; backtrack one level.
                    scratch.DfsPathDepth--;
                }
            }
        }

        // Records a reached crucial node (origin -> reached) for later edge emission, unless the
        // reciprocal edge already exists or this reach is a duplicate. Does NOT drop reaches into
        // already-processed crucials — that edge must be emitted so a second driver's flow reaches
        // the node during propagation; re-enqueuing the processed node is what's suppressed, and
        // that's handled in TryEmitEdge.
        private static void TryRecordReachedCrucial(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState, int originCrucialIdx, int reachedCrucialIdx, int currDepth)
        {
            // A junction is exempt from reciprocal suppression: it has to exist in both directions,
            // because a gate inversion can create or remove it mid-run and which direction got
            // discovered first is an accident of BFS order. Conduction stays governed at run time
            // by DepthStepSystem.EvaluateFlowThroughDiode, so emitting both is safe. Keyed on
            // endpoint cell types rather than adjacency to match the rule that check applies.
            GridCell originCell = GridStackUtility.GetCellDirect(gridStackState, graphState.CrucialNodes[originCrucialIdx].Coord);
            GridCell reachedCell = GridStackUtility.GetCellDirect(gridStackState, graphState.CrucialNodes[reachedCrucialIdx].Coord);
            bool isJunction = IsSwitchableJunction(originCell, reachedCell);

            // Reciprocal suppression: if reached -> origin was already recorded earlier in the
            // build, do not record origin -> reached now. Cumulative across the whole build.
            if (!isJunction && NoReturnContains(scratch, originCrucialIdx, reachedCrucialIdx))
            {
                return;
            }

            // Linear scan for duplicates — DfsReachedCrucialCount is tiny (usually 0-2 per DFS).
            for (int i = 0; i < scratch.DfsReachedCrucialCount; i++)
            {
                if (scratch.DfsReachedCrucial[i] == reachedCrucialIdx) { return; }
            }

            // Snapshot the DFS path NOW. DfsPathBuffer[0..DfsPathDepth] holds the full
            // origin→reached trail at this moment; by the time TryEmitEdge runs (after DFS
            // returns), the buffer has been popped back to just the origin and the
            // intermediate cells are gone. Snapshot is paired with the reached-crucial entry.
            int snapIdx = scratch.DfsReachedCrucialCount;
            int pathLen = scratch.DfsPathDepth;
            int pathStart = ReservePathSlice(graphState, pathLen);
            for (int i = 0; i < pathLen; i++)
            {
                graphState.PathPool[pathStart + i] = scratch.DfsPathBuffer[i];
            }
            scratch.DfsReachedCrucial[snapIdx] = reachedCrucialIdx;
            scratch.DfsReachedPathStart[snapIdx] = pathStart;
            scratch.DfsReachedPathLength[snapIdx] = pathLen;
            scratch.DfsReachedCrucialCount++;

            // Record the reciprocal block: now that origin -> reached exists, forbid reached ->
            // origin. Stored as "reached's no-return list gains origin" so a later reached-origin
            // DFS that reaches origin is suppressed by the NoReturnContains check above. Skipped
            // for junctions, which are deliberately kept bidirectional.
            if (!isJunction)
            {
                NoReturnAdd(scratch, reachedCrucialIdx, originCrucialIdx);
            }

            // Assign BFS depth if not yet assigned. The node's depth is determined the first time
            // it's reached — later reaches from the same or earlier BFS layer don't override it.
            CrucialNode reached = graphState.CrucialNodes[reachedCrucialIdx];
            if (reached.EvalDepth == 0)
            {
                // Possible that a non-input was genuinely already at 0 depth (the default). Safe
                // to overwrite because non-inputs should never be at 0 unless they're already
                // enqueued — in which case we're hitting them again via a later path and the
                // dedupe above already prevented this.
                reached.EvalDepth = currDepth + 1;
                graphState.CrucialNodes[reachedCrucialIdx] = reached;
            }
        }

        // Handles gate-dependency rules and either emits an edge to UnsortedEdges or defers it.
        // Mirrors the prototype's per-edge branching at lines 1142–1216.
        //
        // pathStart / pathLength refer to a slice already written into graphState.PathPool by
        // TryRecordReachedCrucial — capturing it there is necessary because TryEmitEdge runs
        // after DFS has popped its way back, so DfsPathBuffer no longer holds the full path.
        private static void TryEmitEdge(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, GridStackState gridStackState, int originCrucialIdx, int reachedCrucialIdx, int pathStart, int pathLength, int currDepth, int numCols, int cellsPerLayer)
        {
            GridCoord reachedCoord = graphState.CrucialNodes[reachedCrucialIdx].Coord;
            GridCell reachedCell = GridStackUtility.GetCellDirect(gridStackState, reachedCoord);
            TransferType reachedTransfer = reachedCell.TransferType;

            CrucialEdge edge = new CrucialEdge()
            {
                OriginIndex = originCrucialIdx,
                OtherIndex = reachedCrucialIdx,
                EvalDepth = currDepth,
                PathStart = pathStart,
                PathLength = pathLength,
                CycleDetected = false,
            };

            // --- Gate-dependency branches ---------------------------------------------------

            if (reachedTransfer == TransferType.GateBelow)
            {
                // Look up the above-gate: same col/row, metal layer.
                int aboveLayer = (int)StackLayer.Metal;
                int aboveCellIdx = CellIndex(aboveLayer, reachedCoord.Col, reachedCoord.Row, numCols, cellsPerLayer);
                int aboveCrucial = graphState.CellToCrucial[aboveCellIdx];

                bool aboveKnownEvaluated = aboveCrucial >= 0 && scratch.EvaluatedForDependency[aboveCrucial];

                if (aboveKnownEvaluated)
                {
                    // Above-gate already evaluated — safe to emit and enqueue.
                    EnqueueIfUnprocessed(scratch, reachedCrucialIdx);
                    AppendEdge(scratch, edge);
                }
                else if (aboveCrucial >= 0)
                {
                    // Defer: stash the (origin, reached) pair. DO NOT emit the edge now. The
                    // ORIGIN gets re-enqueued at EvalDepth = currDepth + 1 once the above-gate
                    // resolves, and its re-run sweep re-reaches this node and emits the edge at
                    // the correct depth.
                    scratch.AwaitingDependency[reachedCrucialIdx] = true;
                    PushPostponement(scratch, originCrucialIdx, reachedCrucialIdx);

                    // TryRecordReachedCrucial already reserved a path slice for an edge we're
                    // not emitting. Hand it back when it's still the tail of the pool — the
                    // re-run reserves a fresh one. A non-tail slice can't be reclaimed without
                    // compacting, so leave it; the check makes that case a no-op.
                    if (pathStart + pathLength == graphState.PathPoolUsed)
                    {
                        graphState.PathPoolUsed = pathStart;
                    }
                }
                else
                {
                    // No above-gate exists (orphan GateBelow) — treat as normal enqueue.
                    EnqueueIfUnprocessed(scratch, reachedCrucialIdx);
                    AppendEdge(scratch, edge);
                }
            }
            else if (reachedTransfer == TransferType.GateAbove)
            {
                // Mark above-gate evaluated, enqueue it, and resolve any postponements keyed
                // on the matching GateBelow.
                scratch.EvaluatedForDependency[reachedCrucialIdx] = true;
                EnqueueIfUnprocessed(scratch, reachedCrucialIdx);
                AppendEdge(scratch, edge);

                // Look up matching below-gate (same col/row, transistor layer).
                int belowLayer = (int)StackLayer.Transistor;
                int belowCellIdx = CellIndex(belowLayer, reachedCoord.Col, reachedCoord.Row, numCols, cellsPerLayer);
                int belowCrucial = graphState.CellToCrucial[belowCellIdx];

                if (belowCrucial >= 0 && scratch.AwaitingDependency[belowCrucial])
                {
                    ResolvePostponementsForBelow(graphState, scratch, belowCrucial, currDepth);
                    scratch.AwaitingDependency[belowCrucial] = false;
                }
                else if (belowCrucial >= 0 && scratch.DisallowAdditionalDep[belowCrucial])
                {
                    // Below was flushed with DisallowAdditionalDep — any further edges into
                    // it are cycles. Mark the just-emitted edge.
                    scratch.UnsortedEdges[scratch.UnsortedEdgeCount - 1].CycleDetected = true;
                }
            }
            else
            {
                // Normal crucial-to-crucial hop. No gate dependency.
                if (reachedCell.CellType != CellType.Output)
                {
                    EnqueueIfUnprocessed(scratch, reachedCrucialIdx);
                }
                AppendEdge(scratch, edge);
            }
        }

        // Re-enqueue the ORIGIN of each pair postponed on this below-gate, one depth deeper, and
        // remove the resolved pairs. It is the origin's sweep that has to run again: the edge it
        // deferred is only emitted when that sweep re-reaches the below-gate and finds the
        // matching above-gate now marked EvaluatedForDependency. Re-enqueuing the below-gate
        // instead would drop the deferred edge entirely, leaving the gated transistor with no
        // edge from its driver.
        private static void ResolvePostponementsForBelow(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int belowCrucialIdx, int currDepth)
        {
            for (int p = 0; p < scratch.PostponedCount; )
            {
                int dependencyReached = scratch.PostponedPairs[p * 2 + 1];

                if (dependencyReached == belowCrucialIdx)
                {
                    // Bump the origin's depth and enqueue it so its sweep re-runs one layer
                    // deeper. Bypasses EnqueueIfUnprocessed on purpose — the origin has already
                    // been processed, and running it again is the whole point.
                    int postponedOrigin = scratch.PostponedPairs[p * 2];
                    CrucialNode dep = graphState.CrucialNodes[postponedOrigin];
                    dep.EvalDepth = currDepth + 1;
                    graphState.CrucialNodes[postponedOrigin] = dep;
                    PushWork(scratch, postponedOrigin);

                    // Swap-remove this pair from PostponedPairs.
                    int lastP = scratch.PostponedCount - 1;
                    if (p != lastP)
                    {
                        scratch.PostponedPairs[p * 2] = scratch.PostponedPairs[lastP * 2];
                        scratch.PostponedPairs[p * 2 + 1] = scratch.PostponedPairs[lastP * 2 + 1];
                    }
                    scratch.PostponedCount--;
                    // Re-test this index (now holds the last entry).
                }
                else
                {
                    p++;
                }
            }
        }

        // Called when BFS is out of pending work but PostponedPairs isn't empty — their
        // dependencies will never satisfy. For each remaining pair: re-enqueue the ORIGIN at
        // currDepth + 1 so its sweep runs again and emits the edge it deferred, then mark the
        // below-gate DisallowAdditionalDep and its above-gate EvaluatedForDependency so that
        // re-run isn't deferred a second time.
        private static void FlushUnresolvedPostponements(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int currDepth, int numCols, int cellsPerLayer)
        {
            for (int p = 0; p < scratch.PostponedCount; p++)
            {
                int postponedOrigin = scratch.PostponedPairs[p * 2];
                int dependencyReached = scratch.PostponedPairs[p * 2 + 1];

                // Bump the origin to the next depth + re-enqueue.
                CrucialNode dep = graphState.CrucialNodes[postponedOrigin];
                dep.EvalDepth = currDepth + 1;
                graphState.CrucialNodes[postponedOrigin] = dep;
                PushWork(scratch, postponedOrigin);

                scratch.AwaitingDependency[dependencyReached] = false;
                scratch.DisallowAdditionalDep[dependencyReached] = true;

                // Mark matching above-gate (if any) as evaluated so future below references
                // treat it as resolved. Lookup by cellIndex via the below's coord + metal layer.
                GridCoord belowCoord = graphState.CrucialNodes[dependencyReached].Coord;
                int aboveLayer = (int)StackLayer.Metal;
                int aboveCellIdx = CellIndex(aboveLayer, belowCoord.Col, belowCoord.Row, numCols, cellsPerLayer);
                int aboveCrucial = graphState.CellToCrucial[aboveCellIdx];
                if (aboveCrucial >= 0)
                {
                    scratch.EvaluatedForDependency[aboveCrucial] = true;
                }
            }
            scratch.PostponedCount = 0;
        }

        // Append a crucial index to the BFS work queue, growing if needed. WorkTail only ever
        // moves forward (the queue never wraps), and postponement re-queues push nodes that were
        // already processed, so total pushes are not bounded by NodeCount — the growth check is
        // load-bearing, not defensive.
        private static void PushWork(SimulateGraphBuildScratch scratch, int crucialIdx)
        {
            if (scratch.WorkTail >= scratch.WorkQueue.Length)
            {
                int newSize = Mathf.Max(scratch.WorkQueue.Length * 2, scratch.WorkTail + 1);
                Debug.LogWarning("[SimulateGraphUtility] WorkQueue grew from " + scratch.WorkQueue.Length + " to " + newSize + " — initial capacity heuristic undersized");
                Array.Resize(ref scratch.WorkQueue, newSize);
            }
            scratch.WorkQueue[scratch.WorkTail++] = crucialIdx;
        }

        // Append an (origin, gateBelow) postponement pair, growing if needed. One pair per
        // deferred edge, and several origins can defer on the same gate before its above-gate is
        // reached, so this is not bounded by NodeCount either.
        private static void PushPostponement(SimulateGraphBuildScratch scratch, int originCrucialIdx, int belowCrucialIdx)
        {
            int neededInts = (scratch.PostponedCount + 1) * 2;
            if (neededInts > scratch.PostponedPairs.Length)
            {
                int newSize = Mathf.Max(scratch.PostponedPairs.Length * 2, neededInts);
                Debug.LogWarning("[SimulateGraphUtility] PostponedPairs grew from " + scratch.PostponedPairs.Length + " to " + newSize + " — initial capacity heuristic undersized");
                Array.Resize(ref scratch.PostponedPairs, newSize);
            }
            scratch.PostponedPairs[scratch.PostponedCount * 2] = originCrucialIdx;
            scratch.PostponedPairs[scratch.PostponedCount * 2 + 1] = belowCrucialIdx;
            scratch.PostponedCount++;
        }

        // Enqueue a crucial node for BFS processing unless it has already been processed.
        // Re-enqueuing a processed node is what would let the BFS loop forever on mutually-
        // cell-reachable crucials; the edge into it is still emitted by the caller — only the
        // enqueue is suppressed. (Gate postponement deliberately re-enqueues processed nodes
        // via its own paths; those do NOT go through here.)
        private static void EnqueueIfUnprocessed(SimulateGraphBuildScratch scratch, int crucialIdx)
        {
            if (scratch.Processed[crucialIdx]) { return; }
            PushWork(scratch, crucialIdx);
        }

        // True if ownerCrucialIdx's no-return list already contains memberCrucialIdx. Linear scan
        // over the flat (owner, member) pair buffer — NoReturnPairCount is small for real grids.
        private static bool NoReturnContains(SimulateGraphBuildScratch scratch, int ownerCrucialIdx, int memberCrucialIdx)
        {
            for (int i = 0; i < scratch.NoReturnPairCount; i++)
            {
                if (scratch.NoReturnPairs[i * 2] == ownerCrucialIdx
                    && scratch.NoReturnPairs[i * 2 + 1] == memberCrucialIdx)
                {
                    return true;
                }
            }
            return false;
        }

        // Append (owner, member) to the no-return pair buffer, growing if needed. Caller guarantees
        // the pair isn't already present (it's added exactly once, right after an edge is recorded).
        private static void NoReturnAdd(SimulateGraphBuildScratch scratch, int ownerCrucialIdx, int memberCrucialIdx)
        {
            int neededInts = (scratch.NoReturnPairCount + 1) * 2;
            if (neededInts > scratch.NoReturnPairs.Length)
            {
                int newSize = Mathf.Max(scratch.NoReturnPairs.Length * 2, neededInts);
                Debug.LogWarning("[SimulateGraphUtility] NoReturnPairs grew from " + scratch.NoReturnPairs.Length + " to " + newSize + " — initial capacity heuristic undersized");
                Array.Resize(ref scratch.NoReturnPairs, newSize);
            }
            scratch.NoReturnPairs[scratch.NoReturnPairCount * 2] = ownerCrucialIdx;
            scratch.NoReturnPairs[scratch.NoReturnPairCount * 2 + 1] = memberCrucialIdx;
            scratch.NoReturnPairCount++;
        }

        // Append an edge to the UnsortedEdges scratch buffer, resizing if needed.
        private static void AppendEdge(SimulateGraphBuildScratch scratch, CrucialEdge edge)
        {
            if (scratch.UnsortedEdgeCount >= scratch.UnsortedEdges.Length)
            {
                int newSize = Mathf.Max(scratch.UnsortedEdges.Length * 2, scratch.UnsortedEdgeCount + 1);
                Debug.LogWarning("[SimulateGraphUtility] UnsortedEdges grew from " + scratch.UnsortedEdges.Length + " to " + newSize + " — initial capacity heuristic undersized");
                Array.Resize(ref scratch.UnsortedEdges, newSize);
            }
            scratch.UnsortedEdges[scratch.UnsortedEdgeCount++] = edge;
        }

        // Reserve a slice in PathPool, growing if needed.
        //
        // Worked example of PathPool slicing:
        //   Say two edges need to save paths [A, B, C] and [D, E]. Initially PathPoolUsed == 0.
        //   After edge 1: PathPool = [A, B, C, ?, ?, ?, ...], edge1.PathStart = 0, Length = 3,
        //                 PathPoolUsed = 3.
        //   After edge 2: PathPool = [A, B, C, D, E, ?, ...], edge2.PathStart = 3, Length = 2,
        //                 PathPoolUsed = 5.
        //   When DepthStepSystem walks edge1's path, it reads PathPool[0], PathPool[1], PathPool[2].
        //   Edge2: reads PathPool[3], PathPool[4].
        //
        // Returns the start offset into PathPool. Advances PathPoolUsed by pathLength.
        private static int ReservePathSlice(SimulateGraphState graphState, int pathLength)
        {
            if (graphState.PathPoolUsed + pathLength > graphState.PathPool.Length)
            {
                int newSize = Mathf.Max(graphState.PathPool.Length * 2, graphState.PathPoolUsed + pathLength);
                Debug.LogWarning("[SimulateGraphUtility] PathPool grew from " + graphState.PathPool.Length + " to " + newSize + " — initial capacity heuristic undersized");
                Array.Resize(ref graphState.PathPool, newSize);
            }
            int start = graphState.PathPoolUsed;
            graphState.PathPoolUsed += pathLength;
            return start;
        }

        #endregion // Pass 2 helpers

        #region Pass 3 helpers

        // Bucket-sorts scratch.UnsortedEdges into graphState.OrderedEdges by EvalDepth.
        //
        // Why bucket-sort: depths are small (~0-10), so counting sort is O(N + maxDepth) with
        // zero comparisons. Generic sorts would allocate comparers and do unnecessary work.
        //
        // Typical maxDepth fits in a stack buffer; extreme-depth grids fall back to a managed
        // array (allocates, warns). Two code paths keep the stack path fully unmanaged.
        private static void BucketSortEdgesByDepth(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int maxDepth)
        {
            // Ensure OrderedEdges can hold all edges.
            if (graphState.OrderedEdges.Length < scratch.UnsortedEdgeCount)
            {
                int newSize = Mathf.Max(graphState.OrderedEdges.Length * 2, scratch.UnsortedEdgeCount);
                Debug.LogWarning("[SimulateGraphUtility] OrderedEdges grew from " + graphState.OrderedEdges.Length + " to " + newSize + " — initial capacity heuristic undersized");
                Array.Resize(ref graphState.OrderedEdges, newSize);
            }

            int bucketCount = maxDepth + 1;

            // Ensure DepthEdgeStart can hold one entry per bucket + a sentinel at [maxDepth+1].
            EnsureCapacity(ref graphState.DepthEdgeStart, bucketCount + 1, nameof(graphState.DepthEdgeStart));

            const int StackHistogramCap = 64;

            if (bucketCount <= StackHistogramCap)
            {
                BucketSortWithStackHistogram(graphState, scratch, bucketCount);
            }
            else
            {
                Debug.LogWarning("[SimulateGraphUtility] maxDepth " + maxDepth + " exceeds StackHistogramCap; falling back to heap histogram");
                BucketSortWithHeapHistogram(graphState, scratch, bucketCount);
            }

            graphState.EdgeCount = scratch.UnsortedEdgeCount;

            // DepthEdgeStart[maxDepth+1] is the sentinel consumed by DepthStepSystem's
            // "edges at depth d are [DepthEdgeStart[d], DepthEdgeStart[d+1])" slice math.
            graphState.DepthEdgeStart[bucketCount] = graphState.EdgeCount;
        }

        private static unsafe void BucketSortWithStackHistogram(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int bucketCount)
        {
            int* counts = stackalloc int[bucketCount];
            for (int i = 0; i < bucketCount; i++) { counts[i] = 0; }

            for (int i = 0; i < scratch.UnsortedEdgeCount; i++)
            {
                counts[scratch.UnsortedEdges[i].EvalDepth]++;
            }

            int running = 0;
            for (int d = 0; d < bucketCount; d++)
            {
                int c = counts[d];
                counts[d] = running;
                // Snapshot the per-depth start BEFORE the placement pass mutates counts[d] as
                // a write cursor. After this loop, DepthEdgeStart[d] is the first index in
                // OrderedEdges at depth d.
                graphState.DepthEdgeStart[d] = running;
                running += c;
            }

            for (int i = 0; i < scratch.UnsortedEdgeCount; i++)
            {
                int d = scratch.UnsortedEdges[i].EvalDepth;
                graphState.OrderedEdges[counts[d]++] = scratch.UnsortedEdges[i];
            }
        }

        private static void BucketSortWithHeapHistogram(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int bucketCount)
        {
            int[] counts = new int[bucketCount];

            for (int i = 0; i < scratch.UnsortedEdgeCount; i++)
            {
                counts[scratch.UnsortedEdges[i].EvalDepth]++;
            }

            int running = 0;
            for (int d = 0; d < bucketCount; d++)
            {
                int c = counts[d];
                counts[d] = running;
                // Snapshot per-depth start before placement mutates counts[d]. See
                // BucketSortWithStackHistogram for the full comment.
                graphState.DepthEdgeStart[d] = running;
                running += c;
            }

            for (int i = 0; i < scratch.UnsortedEdgeCount; i++)
            {
                int d = scratch.UnsortedEdges[i].EvalDepth;
                graphState.OrderedEdges[counts[d]++] = scratch.UnsortedEdges[i];
            }
        }

        #endregion // Pass 3 helpers

        #region Shared helpers

        // Convert (layer, col, row) to a flat int in [0, cellCount). Inverse is available by
        // computing layer = idx / cellsPerLayer; within-layer remainder = idx % cellsPerLayer;
        // row = remainder / numCols; col = remainder % numCols.
        private static int CellIndex(int layer, int col, int row, int numCols, int cellsPerLayer)
        {
            return layer * cellsPerLayer + row * numCols + col;
        }

        // Ensure an array has at least the requested capacity. Lazy-allocates on first call;
        // otherwise resizes (with warning) when undersized. Never shrinks.
        private static void EnsureCapacity<T>(ref T[] arr, int required, string name)
        {
            if (arr == null)
            {
                arr = new T[required];
                return;
            }
            if (arr.Length < required)
            {
                Debug.LogWarning("[SimulateGraphUtility] " + name + " grew from " + arr.Length + " to " + required + " — initial capacity heuristic undersized");
                Array.Resize(ref arr, required);
            }
        }

        #endregion // Shared helpers
    }
}

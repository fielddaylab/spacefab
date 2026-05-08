using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// One "crucial" node in the evaluation graph: an Input, Output, Gate (above/below), P-N
    /// transition, or dangling-end cell. All identity is integer — CellIndex (into GridStackState)
    /// and CrucialIndex (this node's slot in SimulateGraphState.CrucialNodes).
    ///
    /// EvalDepth is the BFS layer at which this node gets its flow value computed. Inputs are
    /// depth 0; each crucial-to-crucial hop adds 1. Gate postponement can bump a node's depth
    /// higher after initial discovery — see Pass 2 of SimulateGraphUtility.Build.
    ///
    /// FirstOutEdgeIndex / OutEdgeCount index into SimulateGraphState.OrderedEdges. Populated in
    /// Pass 4 as a byproduct of feeding DependencySolver; left zero if that pass is skipped.
    /// </summary>
    public struct CrucialNode
    {
        public int CellIndex;
        public GridCoord Coord;
        public int EvalDepth;
        public int FirstOutEdgeIndex;
        public int OutEdgeCount;
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
    /// CycleDetected is set by Pass 4 when the edge participates in a cycle (back edge under
    /// DFS coloring). The propagation loop uses this to mark cells along the path as unstable.
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
        [HideInInspector] public bool IsBuilt;

        // Node table: 0..NodeCount-1 are valid. Array is sized to an upper bound (cellCount) and
        // not resized until a build hits a larger grid, at which point Build grows it.
        [HideInInspector] public CrucialNode[] CrucialNodes;
        [HideInInspector] public int NodeCount;

        // Edge table, depth-sorted. 0..EdgeCount-1 are valid. All crucial edges are written here
        // AFTER the bucket-sort in Pass 3 — never appended directly during BFS.
        [HideInInspector] public CrucialEdge[] OrderedEdges;
        [HideInInspector] public int EdgeCount;

        // Largest EvalDepth across OrderedEdges. Used by SimulateModeSystem.ProcessPropagating
        // to decide when the propagation walk has finished (CurrentDepth > MaxDepth).
        [HideInInspector] public int MaxDepth;

        // Shared path pool: every edge.PathStart/PathLength slices into this flat int[] of
        // cellIndices. Durable (not scratch) because DepthStepSystem reads from it every
        // frame during Propagating.
        [HideInInspector] public int[] PathPool;
        [HideInInspector] public int PathPoolUsed;

        // cellIndex → crucialIndex reverse lookup. Entries of -1 mean "this cell is not a crucial
        // node." Sized to cellCount and reset per build.
        [HideInInspector] public int[] CellToCrucial;

        // Per-depth edge range table. DepthEdgeStart[d] is the first index in OrderedEdges whose
        // EvalDepth == d; DepthEdgeStart[MaxDepth + 1] == EdgeCount (sentinel). Lets
        // DepthStepSystem iterate exactly the edges at CurrentDepth without scanning the full
        // edge list. Populated by Pass 3 as a byproduct of the bucket-sort prefix-sum.
        [HideInInspector] public int[] DepthEdgeStart;

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
        private const int InitialEdgesPerCell = 4;
        private const int InitialPathEntriesPerCell = 4;

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
        /// Pass 4: detect cycles via index-based coloring DFS; mark back-edges as CycleDetected.
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
            Pass4_DetectCycles(graphState, scratch);
            Pass5_Finalize(graphState, maxDepth);
        }

        #region Pass 0

        // ====================================================================================
        // PASS 0 — Capacity hint + state reset
        // ------------------------------------------------------------------------------------
        // Lazy-allocate every array on first run; on subsequent runs resize only if the
        // grid has grown. Growth is loud (Debug.LogWarning) so undersized initial
        // heuristics become visible during playtesting.
        //
        // Resets all counts + stamp-invalidates visited/no-return marks. The stamp-reset
        // trick avoids clearing VisitStamps/NoReturnStamps with an O(cellCount) loop —
        // we bump the stamp and any prior marks become stale. CellToCrucial must still be
        // cleared to -1 because its sentinel value is relied on everywhere downstream.
        // ====================================================================================
        private static void Pass0_EnsureCapacityAndReset(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int cellCount)
        {
            EnsureCapacity(ref graphState.CellToCrucial, cellCount, nameof(graphState.CellToCrucial));
            EnsureCapacity(ref graphState.CrucialNodes, cellCount, nameof(graphState.CrucialNodes));
            EnsureCapacity(ref graphState.OrderedEdges, cellCount * InitialEdgesPerCell, nameof(graphState.OrderedEdges));
            EnsureCapacity(ref graphState.PathPool, cellCount * InitialPathEntriesPerCell, nameof(graphState.PathPool));

            EnsureCapacity(ref scratch.CellAdjStart, cellCount, nameof(scratch.CellAdjStart));
            EnsureCapacity(ref scratch.CellAdjCount, cellCount, nameof(scratch.CellAdjCount));
            EnsureCapacity(ref scratch.CellAdjDest, cellCount * 6, nameof(scratch.CellAdjDest));
            EnsureCapacity(ref scratch.WorkQueue, cellCount, nameof(scratch.WorkQueue));
            EnsureCapacity(ref scratch.VisitStamps, cellCount, nameof(scratch.VisitStamps));
            EnsureCapacity(ref scratch.NoReturnStamps, cellCount, nameof(scratch.NoReturnStamps));
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

            graphState.NodeCount = 0;
            graphState.EdgeCount = 0;
            graphState.MaxDepth = 0;
            graphState.PathPoolUsed = 0;

            scratch.CellAdjDestUsed = 0;
            scratch.WorkHead = 0;
            scratch.WorkTail = 0;
            scratch.DfsPathDepth = 0;
            scratch.DfsReachedCrucialCount = 0;
            scratch.PostponedCount = 0;
            scratch.UnsortedEdgeCount = 0;

            // Bump stamps: a single int increment invalidates every prior "visited" / "no return"
            // mark in O(1). Much cheaper than walking cellCount entries to clear them to zero.
            scratch.CurrentVisitStamp++;
            scratch.CurrentNoReturnStamp++;

            // Clear CellToCrucial so cells without crucial assignments show -1. This IS an O(cellCount)
            // pass, but -1 is the sentinel we rely on everywhere downstream — cannot use a stamp.
            for (int i = 0; i < cellCount; i++)
            {
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
                                FirstOutEdgeIndex = 0,
                                OutEdgeCount = 0,
                            };
                            graphState.CellToCrucial[cellIndex] = crucialIdx;
                            graphState.NodeCount++;

                            // Inputs are the BFS seed set. Everything else is reached via BFS
                            // and gets its real EvalDepth assigned at discovery time.
                            if (cell.CellType == CellType.Input)
                            {
                                scratch.WorkQueue[scratch.WorkTail++] = crucialIdx;
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
        //     if its EvalDepth differs from prior depth, bump CurrentVisitStamp (new layer)
        //     bump CurrentNoReturnStamp (fresh no-return set for this origin)
        //     for each outgoing neighbor of currCellIdx:
        //        DFS through non-crucial cells to find reachable crucial nodes
        //        for each reached crucial: emit a CrucialEdge (with path slice copied into
        //            PathPool) unless gate-dependency rules defer it
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

                // Mark this crucial node as processed so future DFS reaches don't re-record
                // it (which would re-enqueue it via TryEmitEdge and infinite-loop on any pair
                // of mutually-cell-reachable crucials).
                scratch.Processed[currCrucialIdx] = true;

                // Depth boundary: bump the visit stamp so DFS state from the previous layer
                // doesn't leak into this one. The prototype did a full ResetAllVisited here;
                // the stamp bump is O(1).
                if (nodeDepth != currDepth)
                {
                    scratch.CurrentVisitStamp++;
                    currDepth = nodeDepth;
                }

                // Fresh no-return set for this origin: crucial nodes this DFS reaches shouldn't
                // be allowed to DFS back into us when they later process. Same stamp trick.
                scratch.CurrentNoReturnStamp++;

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
                    DfsFromNeighbor(graphState, scratch, currCellIdx, neighborCellIdx, currDepth);

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

        #region Pass 4

        // ====================================================================================
        // PASS 4 — Cycle detection
        // ------------------------------------------------------------------------------------
        // DependencySolver.Solve exists in the codebase for topological ordering + cycle
        // detection, but it returns a single boolean Result on cycle — no per-node or per-
        // edge attribution. Since the propagation loop's `stable &= !currEdge.CycleDetected`
        // check NEEDS per-edge attribution, we do an index-based 3-color DFS directly on
        // OrderedEdges instead.
        //
        // Coloring scheme (white = 0 = unvisited; gray = 1 = on current DFS stack;
        // black = 2 = fully explored). A back-edge is any edge whose target is gray. When
        // we find a back-edge, we mark its edge.CycleDetected = true and continue — we do
        // not abort because we want to find ALL back-edges, not just the first.
        // ====================================================================================
        private static void Pass4_DetectCycles(SimulateGraphState graphState, SimulateGraphBuildScratch scratch)
        {
            BuildOutEdgeOffsets(graphState);
            DetectCyclesAndMarkBackEdges(graphState, scratch);
        }

        #endregion // Pass 4

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

        // ====================================================================================
        // Shared helpers used across passes follow below. They're grouped by which pass invokes
        // them; each pass method above delegates to the helpers in the matching region.
        // ====================================================================================

        #region Pass 1 helpers

        // True if this P/N transistor cell borders a transistor of the opposite type via any
        // connected edge. Mirrors prototype's IsTransistorTransition (lines 1010–1039) but
        // takes the cell as a parameter instead of doing a second lookup, and takes explicit
        // layer/dim args instead of reading GridStack.Instance.
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
                if (cell.CellType == CellType.NTransistor && adjCell.CellType == CellType.PTransistor) { return true; }
                if (cell.CellType == CellType.PTransistor && adjCell.CellType == CellType.NTransistor) { return true; }
            }

            return false;
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
        private static unsafe void DfsFromNeighbor(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int originCellIdx, int firstNeighborCellIdx, int currDepth)
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
                TryRecordReachedCrucial(graphState, scratch, firstCrucial, originCellIdx, currDepth);
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

                    // Already visited this DFS? Skip.
                    if (scratch.VisitStamps[dest] == scratch.CurrentVisitStamp) { continue; }

                    // Blocked by a previous origin's no-return set? Skip (prototype's NoReturnList).
                    if (scratch.NoReturnStamps[dest] == scratch.CurrentNoReturnStamp) { continue; }

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
                        TryRecordReachedCrucial(graphState, scratch, destCrucial, originCellIdx, currDepth);
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

        // If this crucial index isn't already in DfsReachedCrucial AND the origin hasn't marked
        // its cell as no-return for this origin stamp, add it to the reached list and mark the
        // origin as no-return on the reached cell.
        private static void TryRecordReachedCrucial(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int reachedCrucialIdx, int originCellIdx, int currDepth)
        {
            // Skip already-processed crucials. Without this, the cell-graph's symmetric adjacency
            // makes the BFS infinite-loop on any pair of mutually-reachable crucials (a metal
            // endpoint can DFS back to its driving Input, which would re-enqueue the Input,
            // which would re-DFS to the endpoint, etc.). Dropping the back-edge here means
            // Pass 4's cycle detection won't see true logical cycles either — that's a known
            // tradeoff and a separate concern from the queue-overflow bug.
            if (scratch.Processed[reachedCrucialIdx])
            {
                return;
            }

            int reachedCellIdx = graphState.CrucialNodes[reachedCrucialIdx].CellIndex;

            // Don't allow bouncing back to origin via any subsequent DFS.
            if (scratch.NoReturnStamps[reachedCellIdx] == scratch.CurrentNoReturnStamp)
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

            // Tag origin cell as no-return for this reached crucial node's later DFS.
            scratch.NoReturnStamps[originCellIdx] = scratch.CurrentNoReturnStamp;

            // Assign BFS depth if not yet assigned. The node's depth is determined the first time
            // it's reached — later reaches from the same or earlier BFS layer don't override it.
            // Prototype sets parentDepth + 1 here.
            CrucialNode reached = graphState.CrucialNodes[reachedCrucialIdx];
            if (reached.EvalDepth == 0)
            {
                // Possible that a non-input was genuinely already at 0 depth (the default). Safe
                // to overwrite because non-inputs should never be at 0 unless they're already
                // enqueued — in which case we're hitting them again via a later path and the
                // ContainsByName-style dedupe above already prevented this.
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
                    scratch.WorkQueue[scratch.WorkTail++] = reachedCrucialIdx;
                    AppendEdge(scratch, edge);
                }
                else if (aboveCrucial >= 0)
                {
                    // Defer: stash the (origin, reached) pair. DO NOT emit the edge now. The
                    // reached node will be re-enqueued with EvalDepth = currDepth + 1 when the
                    // above-gate resolves, and this same DFS will re-run to re-emit the edge
                    // at the correct depth.
                    scratch.AwaitingDependency[reachedCrucialIdx] = true;
                    scratch.PostponedPairs[scratch.PostponedCount * 2] = originCrucialIdx;
                    scratch.PostponedPairs[scratch.PostponedCount * 2 + 1] = reachedCrucialIdx;
                    scratch.PostponedCount++;
                    // Note: TryRecordReachedCrucial already reserved the path slice; on a
                    // defer we hold that PathPool slot without using it. Wasteful in rare
                    // pathological layouts; consider pool-rewind-on-defer if profiling shows
                    // the pool bloating.
                }
                else
                {
                    // No above-gate exists (orphan GateBelow) — treat as normal enqueue.
                    scratch.WorkQueue[scratch.WorkTail++] = reachedCrucialIdx;
                    AppendEdge(scratch, edge);
                }
            }
            else if (reachedTransfer == TransferType.GateAbove)
            {
                // Mark above-gate evaluated, enqueue it, and resolve any postponements keyed
                // on the matching GateBelow.
                scratch.EvaluatedForDependency[reachedCrucialIdx] = true;
                scratch.WorkQueue[scratch.WorkTail++] = reachedCrucialIdx;
                AppendEdge(scratch, edge);

                // Look up matching below-gate (same col/row, transistor layer).
                int belowLayer = (int)StackLayer.Transistor;
                int belowCellIdx = CellIndex(belowLayer, reachedCoord.Col, reachedCoord.Row, numCols, cellsPerLayer);
                int belowCrucial = graphState.CellToCrucial[belowCellIdx];

                if (belowCrucial >= 0 && scratch.AwaitingDependency[belowCrucial])
                {
                    ResolvePostponementsForBelow(graphState, scratch, belowCrucial, belowCellIdx, currDepth);
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
                scratch.WorkQueue[scratch.WorkTail++] = reachedCrucialIdx;
                AppendEdge(scratch, edge);
            }
        }

        // Re-enqueue postponed dependents at one depth deeper, reset the below-cell's visit
        // stamp so DFS can hit it again next depth layer, and remove the resolved pairs.
        private static void ResolvePostponementsForBelow(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int belowCrucialIdx, int belowCellIdx, int currDepth)
        {
            for (int p = 0; p < scratch.PostponedCount; )
            {
                int dependencyReached = scratch.PostponedPairs[p * 2 + 1];

                if (dependencyReached == belowCrucialIdx)
                {
                    // Bump the reached node's depth, enqueue it, reset the below cell's visit
                    // stamp to allow DFS to re-find the path on the next layer.
                    CrucialNode dep = graphState.CrucialNodes[dependencyReached];
                    dep.EvalDepth = currDepth + 1;
                    graphState.CrucialNodes[dependencyReached] = dep;
                    scratch.WorkQueue[scratch.WorkTail++] = dependencyReached;
                    scratch.VisitStamps[belowCellIdx] = 0;

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
        // dependencies will never satisfy. For each remaining pair: push dependent at
        // currDepth + 1, mark below cell DisallowAdditionalDep, mark above cell
        // EvaluatedForDependency. Mirrors prototype lines 1230–1259.
        private static void FlushUnresolvedPostponements(SimulateGraphState graphState, SimulateGraphBuildScratch scratch, int currDepth, int numCols, int cellsPerLayer)
        {
            for (int p = 0; p < scratch.PostponedCount; p++)
            {
                int dependencyReached = scratch.PostponedPairs[p * 2 + 1];

                // Bump the dependent to the next depth + re-enqueue.
                CrucialNode dep = graphState.CrucialNodes[dependencyReached];
                dep.EvalDepth = currDepth + 1;
                graphState.CrucialNodes[dependencyReached] = dep;
                scratch.WorkQueue[scratch.WorkTail++] = dependencyReached;

                int belowCellIdx = graphState.CrucialNodes[dependencyReached].CellIndex;
                scratch.VisitStamps[belowCellIdx] = 0;

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

        #region Pass 4 helpers

        // Populate FirstOutEdgeIndex / OutEdgeCount on each CrucialNode based on OrderedEdges.
        // Because OrderedEdges is sorted by EvalDepth (not by origin), we need two sweeps: one
        // to count per-origin, one to compute starts. Then a scatter pass to produce a
        // contiguous per-origin view... but that would require shuffling OrderedEdges, which
        // breaks depth ordering.
        //
        // Compromise: use a per-origin edge-INDEX table (scratch field) that maps origin ->
        // List<edgeIdx>. For cycle detection we don't actually need contiguity — just the set
        // of outgoing edge indices per node. We scan OrderedEdges once per DFS to find outgoing
        // edges of a given origin. Keep the sweep cheap (EdgeCount is small).
        //
        // For now, leave FirstOutEdgeIndex / OutEdgeCount at zero unless a future consumer
        // needs them. Cycle detection will use an ad-hoc O(edges) scan per node, which is fine
        // given typical edge counts in the hundreds.
        private static void BuildOutEdgeOffsets(SimulateGraphState graphState)
        {
            // No-op for now — left as TODO for future performance work if cycle detection
            // or other consumers need per-node out-edge contiguity. The current coloring DFS
            // iterates OrderedEdges linearly and filters by origin, which is acceptable.
        }

        // Three-color DFS cycle detection, marking back-edges as CycleDetected.
        //
        // white = 0 (unvisited), gray = 1 (on current DFS stack), black = 2 (fully explored).
        // A back-edge is any edge whose target is currently gray. When found, mark
        // edge.CycleDetected = true and continue — we want ALL back-edges, not just the first.
        //
        // The outer loop iterates each node as a potential DFS root to cover disconnected
        // components. Inner stack is explicit, keyed by OrderedEdges cursor per frame.
        //
        // Stack-path via stackalloc for node counts up to StackColorCap. Extreme grids fall
        // back to a managed implementation. Split code paths to keep the stack path clean.
        private static void DetectCyclesAndMarkBackEdges(SimulateGraphState graphState, SimulateGraphBuildScratch scratch)
        {
            int nodeCount = graphState.NodeCount;
            if (nodeCount == 0) { return; }

            const int StackColorCap = 256;
            if (nodeCount <= StackColorCap)
            {
                DetectCyclesStackPath(graphState, nodeCount, StackColorCap);
            }
            else
            {
                Debug.LogWarning("[SimulateGraphUtility] nodeCount " + nodeCount + " exceeds StackColorCap; falling back to heap cycle detection");
                DetectCyclesHeapPath(graphState, nodeCount);
            }
        }

        private static unsafe void DetectCyclesStackPath(SimulateGraphState graphState, int nodeCount, int stackCap)
        {
            byte* colors = stackalloc byte[nodeCount];
            for (int i = 0; i < nodeCount; i++) { colors[i] = 0; }
            int* stackNodes = stackalloc int[stackCap];
            int* stackCursors = stackalloc int[stackCap];

            for (int startNode = 0; startNode < nodeCount; startNode++)
            {
                if (colors[startNode] != 0) { continue; }

                int stackDepth = 0;
                stackNodes[stackDepth] = startNode;
                stackCursors[stackDepth] = 0;
                colors[startNode] = 1;
                stackDepth++;

                while (stackDepth > 0)
                {
                    int frame = stackDepth - 1;
                    int currNode = stackNodes[frame];
                    int cursor = stackCursors[frame];

                    int foundEdgeIdx = FindNextOutEdge(graphState, currNode, cursor);
                    stackCursors[frame] = foundEdgeIdx < 0 ? graphState.EdgeCount : foundEdgeIdx + 1;

                    if (foundEdgeIdx < 0)
                    {
                        colors[currNode] = 2;
                        stackDepth--;
                        continue;
                    }

                    int otherIdx = graphState.OrderedEdges[foundEdgeIdx].OtherIndex;
                    byte otherColor = colors[otherIdx];

                    if (otherColor == 1)
                    {
                        graphState.OrderedEdges[foundEdgeIdx].CycleDetected = true;
                    }
                    else if (otherColor == 0)
                    {
                        if (stackDepth >= stackCap)
                        {
                            Debug.LogWarning("[SimulateGraphUtility] cycle-detection stack overflow; aborting");
                            return;
                        }
                        colors[otherIdx] = 1;
                        stackNodes[stackDepth] = otherIdx;
                        stackCursors[stackDepth] = 0;
                        stackDepth++;
                    }
                }
            }
        }

        private static void DetectCyclesHeapPath(SimulateGraphState graphState, int nodeCount)
        {
            byte[] colors = new byte[nodeCount];
            int[] stackNodes = new int[nodeCount];
            int[] stackCursors = new int[nodeCount];

            for (int startNode = 0; startNode < nodeCount; startNode++)
            {
                if (colors[startNode] != 0) { continue; }

                int stackDepth = 0;
                stackNodes[stackDepth] = startNode;
                stackCursors[stackDepth] = 0;
                colors[startNode] = 1;
                stackDepth++;

                while (stackDepth > 0)
                {
                    int frame = stackDepth - 1;
                    int currNode = stackNodes[frame];
                    int cursor = stackCursors[frame];

                    int foundEdgeIdx = FindNextOutEdge(graphState, currNode, cursor);
                    stackCursors[frame] = foundEdgeIdx < 0 ? graphState.EdgeCount : foundEdgeIdx + 1;

                    if (foundEdgeIdx < 0)
                    {
                        colors[currNode] = 2;
                        stackDepth--;
                        continue;
                    }

                    int otherIdx = graphState.OrderedEdges[foundEdgeIdx].OtherIndex;
                    byte otherColor = colors[otherIdx];

                    if (otherColor == 1)
                    {
                        graphState.OrderedEdges[foundEdgeIdx].CycleDetected = true;
                    }
                    else if (otherColor == 0)
                    {
                        colors[otherIdx] = 1;
                        stackNodes[stackDepth] = otherIdx;
                        stackCursors[stackDepth] = 0;
                        stackDepth++;
                    }
                }
            }
        }

        // Scan OrderedEdges starting at cursor for the next edge whose OriginIndex == node.
        // Returns the edge index, or -1 if no more.
        private static int FindNextOutEdge(SimulateGraphState graphState, int node, int cursor)
        {
            while (cursor < graphState.EdgeCount)
            {
                if (graphState.OrderedEdges[cursor].OriginIndex == node) { return cursor; }
                cursor++;
            }
            return -1;
        }

        #endregion // Pass 4 helpers

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

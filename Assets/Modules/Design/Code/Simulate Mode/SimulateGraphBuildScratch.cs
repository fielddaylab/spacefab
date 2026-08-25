using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Transient workspace used exclusively by SimulateGraphUtility.Build. Every array is lazy-allocated
    /// on first use and reused across subsequent builds (counts reset, arrays kept). This is the entire
    /// reason Build runs with zero steady-state GC pressure: nothing here is created per invocation.
    ///
    /// None of these fields are read outside Build. SimulateGraphState holds the durable graph output
    /// (node table, depth-sorted edge table, path pool); this component holds the BFS work queue, DFS
    /// visit stamps, postponement queue, and per-cell adjacency scratch needed to produce that output.
    ///
    /// Hidden from the inspector by design — exposing scratch would be noise for developers and invites
    /// confusion about what state is durable vs. transient.
    /// </summary>
    public class SimulateGraphBuildScratch : SharedStateComponent, IRegistrationCallbacks
    {
        // ---- Per-cell adjacency (replaces Dictionary<GraphCoord,GraphNode> + GraphNode.Edges) ----
        //
        // Flat representation of the 6-directional edge graph across every non-empty cell in the
        // grid. We record connections for every non-empty cell — not only crucial cells — because
        // DFS walks through intermediate cells to discover paths between crucial nodes.
        //
        // CellAdjStart[cellIdx]  — first index in CellAdjDest for this cell's outgoing neighbors.
        // CellAdjCount[cellIdx]  — number of valid entries (0..6).
        // CellAdjDest[k]         — the cellIndex of the k-th connected neighbor.
        // CellAdjDestUsed        — total entries written into CellAdjDest so far this build.

        [HideInInspector] public int[] CellAdjStart;
        [HideInInspector] public int[] CellAdjCount;
        [HideInInspector] public int[] CellAdjDest;
        [HideInInspector] public int CellAdjDestUsed;

        // ---- BFS work queue of crucial-node indices ----
        //
        // WorkQueue is a plain circular-free queue (head chases tail forward-only, never wraps)
        // sized to cellCount, which is a safe upper bound on crucial-node count. Nodes are
        // enqueued multiple times during the build: first on initial discovery, then potentially
        // re-enqueued at a later EvalDepth when a gate-dependency postponement resolves.

        [HideInInspector] public int[] WorkQueue;
        [HideInInspector] public int WorkHead;
        [HideInInspector] public int WorkTail;

        // ---- DFS visit stamps (O(1) reset replacement for a bool[]) ----
        //
        // Instead of clearing a bool[cellCount] between DFS runs, we bump CurrentVisitStamp and
        // consider a cell visited iff VisitStamps[cellIdx] == CurrentVisitStamp. Clearing is a
        // single int increment, not a loop over every cell. Wraparound is a theoretical concern
        // but would require running billions of DFS passes in one Build — not a real risk here.
        //
        // The stamp is bumped once per crucial node DEQUEUED in Pass 2, so visited is scoped to
        // a single origin's sweep. It is deliberately NOT shared across a depth layer: sharing
        // lets whichever node is dequeued first claim a shared metal region, and every other
        // crucial node on that region then finds its first neighbour stamped and emits no edge
        // into it at all. Per-origin scoping is also what makes CrucialNode.EvalDepth come out
        // as the exact hop distance from the Input set.

        [HideInInspector] public int[] VisitStamps;
        [HideInInspector] public int CurrentVisitStamp;

        // ---- DFS path scratch (shared by all DFS invocations within a Build) ----
        //
        // DfsPathBuffer doubles as the recursion stack AND the current path-in-progress. When we
        // reach a crucial node, we snapshot DfsPathBuffer[0..DfsPathDepth] into PathPool and
        // record the slice on the emitted CrucialEdge.

        [HideInInspector] public int[] DfsPathBuffer;
        [HideInInspector] public int DfsPathDepth;

        // ---- Crucial nodes reached during one DFS invocation ----
        //
        // Populated as DFS encounters crucial cells downstream of the current origin. After DFS
        // on one outgoing neighbor finishes, Pass 2 walks this list to emit one CrucialEdge per
        // reached crucial node. Reset (DfsReachedCrucialCount = 0) at the start of each DFS run.
        //
        // DfsReachedPathStart / DfsReachedPathLength are parallel arrays storing the path slice
        // captured at TryRecordReachedCrucial time (when DfsPathBuffer holds the full origin→
        // reached trail). TryEmitEdge later uses these instead of re-snapshotting from the
        // already-backtracked DfsPathBuffer.

        [HideInInspector] public int[] DfsReachedCrucial;
        [HideInInspector] public int[] DfsReachedPathStart;
        [HideInInspector] public int[] DfsReachedPathLength;
        [HideInInspector] public int DfsReachedCrucialCount;

        // ---- Postponed gate-dependency pairs ----
        //
        // When DFS lands on a GateBelow whose matching GateAbove has not yet been evaluated,
        // we defer emitting that edge until the dependency is satisfied. Each postponement
        // records (dependentOriginCrucialIdx, dependencyGateBelowCrucialIdx) — two ints per
        // pair, packed into a single array to avoid allocating a 2-int struct.
        //
        // BOTH slots are read when a postponement resolves. Slot 0 (the origin) is what gets
        // re-enqueued a depth deeper, because only the origin's sweep can emit the edge it
        // deferred. Slot 1 (the gate-below) is what carries AwaitingDependency /
        // DisallowAdditionalDep and locates the matching gate-above.
        //
        // PostponedCount is the number of PAIRS, not the number of ints. Total ints = 2 * count.

        [HideInInspector] public int[] PostponedPairs;
        [HideInInspector] public int PostponedCount;

        // ---- Per-crucial-node transient flags (indexed by crucialIndex, not cellIndex) ----
        //
        // These mirror the prototype's CrucialGraphNode fields by the same names. Only relevant
        // during Build — do not belong on the durable CrucialNode struct. Cleared per build.

        [HideInInspector] public bool[] AwaitingDependency;
        [HideInInspector] public bool[] EvaluatedForDependency;
        [HideInInspector] public bool[] DisallowAdditionalDep;

        // Set to true when a crucial node is dequeued in Pass 2's BFS. Read by TryEmitEdge to
        // avoid RE-ENQUEUING an already-processed crucial (which would let the BFS loop forever
        // on mutually-cell-reachable crucials). It no longer gates edge EMISSION — an edge into
        // an already-processed crucial must still be recorded so a second driver's flow reaches
        // that node during propagation; only the enqueue is suppressed. See SimulateGraphUtility.Pass2.

        [HideInInspector] public bool[] Processed;

        // ---- Cumulative no-return pairs (faithful port of the prototype's per-node NoReturnList) ----
        //
        // Suppresses reciprocal crucial-to-crucial edges: once an edge O→C is recorded, the
        // reverse edge C→O must never be recorded. The prototype stored this as a per-node
        // List<GraphCoord> that is NEVER reset across the build (EvaluationMgr.CrucialGraphNode.
        // NoReturnList). We store the same relation as a flat append-only buffer of (owner, member)
        // crucial-index pairs meaning "owner's no-return list contains member": recording O→C
        // appends (C, O); before recording O→C we skip if the pair (O, C) is already present.
        //
        // NodeCount is tens at most and each node accumulates few entries, so the linear scan in
        // NoReturnContains is cheap. PairCount counts PAIRS; total ints used = 2 * PairCount.
        // This must be cumulative (not stamp-reset-per-dequeue) — a per-dequeue reset is exactly
        // the defect that forced the old, over-pruning Processed-drops-the-edge workaround.

        [HideInInspector] public int[] NoReturnPairs;
        [HideInInspector] public int NoReturnPairCount;

        // ---- Unsorted edge accumulator (populated in Pass 2, sorted into SimulateGraphState.OrderedEdges in Pass 3) ----
        //
        // Edges cannot be written directly to OrderedEdges as they're discovered because gate
        // postponement can emit edges at a later EvalDepth than edges discovered afterward.
        // Pass 3 bucket-sorts UnsortedEdges[0..UnsortedEdgeCount] into OrderedEdges to restore
        // monotonic depth order.

        [HideInInspector] public CrucialEdge[] UnsortedEdges;
        [HideInInspector] public int UnsortedEdgeCount;

        // ---- Per-segment write cursors (Pass 6) ----
        //
        // Scratch for the counting sort that groups cell indices into SimulateGraphState.
        // SegmentCells. Kept separate from SegmentCellStart so that table keeps holding each
        // segment's START offset, which the run-time paint walk reads every time a segment's
        // flow changes.

        [HideInInspector] public int[] SegmentCursor;

        public void OnRegister()
        {
            // Arrays stay null until first Build — Build will lazy-allocate everything to the right
            // capacity based on the actual grid dimensions. All counts zero by default.
        }

        public void OnDeregister()
        {
        }
    }
}

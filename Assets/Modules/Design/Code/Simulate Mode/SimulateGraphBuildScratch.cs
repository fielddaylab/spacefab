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

        // ---- DFS visit stamps (O(1) reset replacement for a bool[] + ResetAllVisited) ----
        //
        // Instead of clearing a bool[cellCount] between DFS runs, we bump CurrentVisitStamp and
        // consider a cell visited iff VisitStamps[cellIdx] == CurrentVisitStamp. Clearing is a
        // single int increment, not a loop over every cell. Wraparound is a theoretical concern
        // but would require running billions of DFS passes in one Build — not a real risk here.

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

        // Set to true when a crucial node is dequeued in Pass 2's BFS. Read by
        // TryRecordReachedCrucial to drop any DFS-reach of an already-processed crucial
        // (back-edges in the cell-adjacency graph). Without this, the BFS infinite-loops on
        // any pair of mutually-cell-reachable crucials — see SimulateGraphUtility.Pass2.

        [HideInInspector] public bool[] Processed;

        // ---- NoReturn stamps (replaces per-origin List<GraphCoord> NoReturnList in prototype) ----
        //
        // For each DFS origin, we mark cellIndices that must not be revisited via crucial-to-
        // crucial edges when *other* origins later DFS through here. Same stamp trick as
        // VisitStamps: bump CurrentNoReturnStamp to invalidate all prior marks. Check is
        // NoReturnStamps[cellIdx] == CurrentNoReturnStamp.

        [HideInInspector] public int[] NoReturnStamps;
        [HideInInspector] public int CurrentNoReturnStamp;

        // ---- Unsorted edge accumulator (populated in Pass 2, sorted into SimulateGraphState.OrderedEdges in Pass 3) ----
        //
        // Edges cannot be written directly to OrderedEdges as they're discovered because gate
        // postponement can emit edges at a later EvalDepth than edges discovered afterward.
        // Pass 3 bucket-sorts UnsortedEdges[0..UnsortedEdgeCount] into OrderedEdges to restore
        // monotonic depth order.

        [HideInInspector] public CrucialEdge[] UnsortedEdges;
        [HideInInspector] public int UnsortedEdgeCount;

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

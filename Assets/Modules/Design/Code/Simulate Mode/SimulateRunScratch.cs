using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-run transient state for Simulate mode.
    ///
    /// Flow is stored per SEGMENT, not per crucial node: a segment is one conductor at one
    /// potential, so storing a value per node would let one wire hold two of them. Per-cell flow
    /// still exists, but only as the render target — it is always a copy of the owning segment's
    /// value, written as the propagation walk reveals cells.
    ///
    /// All state here is per-test: it gets reset at the start of each row (via stamp bump + small
    /// Array.Clear on the segment and node arrays) and is consumed by DepthStepSystem during
    /// propagation and by ProcessResolvingTest at row end. Visuals read the cell-level flow via
    /// SimulateRunScratchUtility on every refresh.
    ///
    /// Lifetime: arrays are lazy-allocated on first Simulate-mode entry via
    /// SimulateRunScratchUtility.EnsureCapacity, and reused for every subsequent entry. Nothing
    /// here is ever freed.
    /// </summary>
    public class SimulateRunScratch : SharedStateComponent, IRegistrationCallbacks
    {
        // ---- Per-segment flow state ----
        //
        // Indexed by segmentId (0..graphState.SegmentCount). THE unit of flow: a segment is one
        // conductor at one potential, so every cell in it necessarily displays the same value and
        // two drivers feeding one segment must agree or the segment resolves Unstable.
        //
        // Only ever moves Empty → value → Unstable (see AssignSegmentFlow). DepthStepSystem's
        // convergence sweeps rely on that monotonicity to terminate.

        [NonSerialized] public FlowState[] SegmentFlow;

        // ---- Per-crucial-node transient P↔N inversion (replaces CrucialGraphNode.TempTransformedType) ----
        //
        // Indexed by crucialIdx. Only meaningful for cells whose CellType is NTransistor or
        // PTransistor; reads for other types are benign (return CellType.NONE).

        [NonSerialized] public CellType[] NodeTempTransform;

        // ---- Per-cell flow (replaces GridCell.FlowState writes) ----
        //
        // Stamp-gated: a cell's flow is "empty" iff CellFlowStamps[cellIdx] != CurrentFlowStamp.
        // This lets per-test reset be a single int increment (BumpFlowStamp) instead of writing
        // every cell back to Empty. CurrentFlowStamp is shared with CellTempTransform below —
        // they reset together because they share a single BumpFlowStamp — but each has its
        // own per-cell stamp array to track writes independently.
        //
        // Reads go through SimulateRunScratchUtility.GetCellFlow which compares the per-cell
        // stamp against CurrentFlowStamp and returns Empty on mismatch.

        [NonSerialized] public FlowState[] CellFlow;
        [NonSerialized] public int[] CellFlowStamps;
        [NonSerialized] public int CurrentFlowStamp;

        // ---- Per-cell temp-transform (replaces GridCell.TempTransformation writes) ----
        //
        // Shares CurrentFlowStamp with CellFlow but has its OWN per-cell stamp array. This
        // matters: a gate-inversion edge writes temp-transform on a below-cell that may not
        // be on any path, so it shouldn't validate that cell's flow stamp. With separate
        // stamp arrays, writing temp-transform only validates temp-transform reads.

        [NonSerialized] public CellType[] CellTempTransform;
        [NonSerialized] public int[] CellTempTransformStamps;

        // ---- Output flow buffer (pooled, reused across all rows) ----
        //
        // Entry i holds the flow observed on the i-th Output crucial node for the most recent
        // row, in the order Output nodes appear in graphState.CrucialNodes. Sized once on
        // Simulate-mode entry and never resized — the output set is a property of the level,
        // not the row.

        [NonSerialized] public FlowState[] OutputFlowBuffer;
        [NonSerialized] public int OutputCount;

        // ---- Per-test diagnostic flag set by DepthStepSystem ----
        //
        // Mirrors SimulateRunState.IsUnstable, and like it, does NOT decide the verdict — see the
        // note there. Duplicated here because DepthStepSystem already has SimulateRunScratch in
        // its permissions and this avoids broadening to RunState just to flip one bool.

        [NonSerialized] public bool IsUnstable;

        public void OnRegister()
        {
            // Arrays stay null until first EnsureCapacity call.
        }

        public void OnDeregister()
        {
        }
    }

    /// <summary>
    /// Helpers for SimulateRunScratch. Ownership: SimulateModeSystem (ProcessPreparingTest +
    /// ProcessCancelling) bumps the flow stamp; DepthStepSystem writes per-cell flow + temp
    /// transforms; VisualGridCell reads via the Get helpers; ModeTransitionSystem calls
    /// EnsureCapacity + SizeOutputBuffer on Simulate-mode entry.
    /// </summary>
    public static class SimulateRunScratchUtility
    {
        // Ensure per-node and per-cell arrays are sized for the current graph + grid. Called
        // from ModeTransitionSystem on Simulate-mode entry and idempotent on repeat calls.
        //
        // Growth is loud (Debug.LogWarning) so undersized initial heuristics become visible
        // during playtesting — same pattern as SimulateGraphUtility.EnsureCapacity.
        public static void EnsureCapacity(SimulateRunScratch scratch, int nodeCount, int segmentCount, int cellCount)
        {
            EnsureArray(ref scratch.SegmentFlow, segmentCount, nameof(scratch.SegmentFlow));
            EnsureArray(ref scratch.NodeTempTransform, nodeCount, nameof(scratch.NodeTempTransform));

            EnsureArray(ref scratch.CellFlow, cellCount, nameof(scratch.CellFlow));
            EnsureArray(ref scratch.CellFlowStamps, cellCount, nameof(scratch.CellFlowStamps));
            EnsureArray(ref scratch.CellTempTransform, cellCount, nameof(scratch.CellTempTransform));
            EnsureArray(ref scratch.CellTempTransformStamps, cellCount, nameof(scratch.CellTempTransformStamps));
        }

        // Sizes the OutputFlowBuffer to match the current level's output count. Called once per
        // Simulate-mode entry after Build completes and the crucial-node table is populated.
        public static void SizeOutputBuffer(SimulateRunScratch scratch, int outputCount)
        {
            scratch.OutputCount = outputCount;
            EnsureArray(ref scratch.OutputFlowBuffer, outputCount, nameof(scratch.OutputFlowBuffer));
        }

        // Clears per-segment and per-node transient state for a new test. Small pass — both counts
        // are tens at most.
        public static void ClearRunTransients(SimulateRunScratch scratch, int nodeCount, int segmentCount)
        {
            if (scratch.SegmentFlow != null) { Array.Clear(scratch.SegmentFlow, 0, segmentCount); }
            if (scratch.NodeTempTransform != null) { Array.Clear(scratch.NodeTempTransform, 0, nodeCount); }
        }

        // Merges an incoming value into a segment and reports whether the stored value moved.
        //
        // Empty → value, same value → no-op, conflicting value → Unstable, and Unstable absorbs
        // everything after. That lattice is what makes two drivers on one conductor either agree
        // or resolve Unstable no matter which order their edges run in, and its monotonicity is
        // what lets DepthStepSystem's convergence sweeps terminate.
        public static bool AssignSegmentFlow(SimulateRunScratch scratch, int segmentId, FlowState incoming)
        {
            if (segmentId < 0 || incoming == FlowState.Empty) { return false; }

            FlowState current = scratch.SegmentFlow[segmentId];
            if (current == incoming || current == FlowState.Unstable) { return false; }

            scratch.SegmentFlow[segmentId] = current == FlowState.Empty ? incoming : FlowState.Unstable;
            return true;
        }

        // Invalidates all per-cell flow + temp-transform marks in O(1). The next GetCellFlow
        // / GetCellTempTransform call on any cell returns the "empty" sentinel because the
        // cell's stamp is now stale relative to CurrentFlowStamp.
        public static void BumpFlowStamp(SimulateRunScratch scratch)
        {
            scratch.CurrentFlowStamp++;
        }

        // Cell-flow read. Returns FlowState.Empty if the cell's stamp is stale (i.e. the last
        // write happened in a previous test or never at all). Safe to call from the visuals
        // layer on every cell during RefreshAll, including during Tool mode when arrays may
        // be uninitialized or undersized (Simulate mode never entered this session).
        public static FlowState GetCellFlow(SimulateRunScratch scratch, int cellIndex)
        {
            if (scratch.CellFlowStamps == null || cellIndex < 0 || cellIndex >= scratch.CellFlowStamps.Length) { return FlowState.Empty; }
            if (scratch.CellFlowStamps[cellIndex] != scratch.CurrentFlowStamp) { return FlowState.Empty; }
            return scratch.CellFlow[cellIndex];
        }

        // Cell-temp-transform read. Returns CellType.NONE if the cell's temp-transform stamp
        // is stale — own stamp array, tracked independently from CellFlow.
        public static CellType GetCellTempTransform(SimulateRunScratch scratch, int cellIndex)
        {
            if (scratch.CellTempTransformStamps == null || cellIndex < 0 || cellIndex >= scratch.CellTempTransformStamps.Length) { return CellType.NONE; }
            if (scratch.CellTempTransformStamps[cellIndex] != scratch.CurrentFlowStamp) { return CellType.NONE; }
            return scratch.CellTempTransform[cellIndex];
        }

        // Cell-flow write. Validates ONLY the flow stamp on this cell — temp-transform reads
        // for this cell remain NONE unless SetCellTempTransform was also called.
        public static void SetCellFlow(SimulateRunScratch scratch, int cellIndex, FlowState flow)
        {
            scratch.CellFlow[cellIndex] = flow;
            scratch.CellFlowStamps[cellIndex] = scratch.CurrentFlowStamp;
        }

        // Cell-temp-transform write. Validates ONLY the temp-transform stamp on this cell.
        // Gate-inversion edges use this on below-cells that may not appear on any DFS path,
        // so decoupling flow and temp-transform stamps keeps both reads independent.
        public static void SetCellTempTransform(SimulateRunScratch scratch, int cellIndex, CellType type)
        {
            scratch.CellTempTransform[cellIndex] = type;
            scratch.CellTempTransformStamps[cellIndex] = scratch.CurrentFlowStamp;
        }

        // Flat cellIndex from (layer, col, row). Mirrors the internal helper used by
        // SimulateGraphUtility.Build. Exposed publicly so visuals and downstream consumers
        // use the same index scheme the graph was built against.
        //
        // Formula: layer * (numCols * numRows) + row * numCols + col
        public static int CellIndex(int layer, int col, int row, int numCols, int cellsPerLayer)
        {
            return layer * cellsPerLayer + row * numCols + col;
        }

        // Lazy-alloc-or-grow helper. Matches the pattern in SimulateGraphUtility.EnsureCapacity.
        private static void EnsureArray<T>(ref T[] arr, int required, string name)
        {
            if (arr == null)
            {
                arr = new T[required];
                return;
            }
            if (arr.Length < required)
            {
                Debug.LogWarning("[SimulateRunScratchUtility] " + name + " grew from " + arr.Length + " to " + required + " — initial capacity heuristic undersized");
                Array.Resize(ref arr, required);
            }
        }
    }
}

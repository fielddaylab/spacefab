using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-run transient state for Simulate mode. Replaces the prototype's ambient flow state,
    /// which was scattered across CrucialGraphNode.CurrFlowState, CrucialGraphNode.TempTransformedType,
    /// GridCell.FlowState, and GridCell.TempTransformation — all accessed via dictionary lookups
    /// with silent boxing.
    ///
    /// All state here is per-test: it gets reset at the start of each row (via stamp bump + small
    /// Array.Clear on the node arrays) and is consumed by DepthStepSystem during propagation and
    /// by ProcessResolvingTest at row end. Visuals read the cell-level flow via
    /// SimulateRunScratchUtility on every refresh.
    ///
    /// Lifetime: arrays are lazy-allocated on first Simulate-mode entry via
    /// SimulateRunScratchUtility.EnsureCapacity, and reused for every subsequent entry. Nothing
    /// here is ever freed.
    /// </summary>
    public class SimulateRunScratch : SharedStateComponent, IRegistrationCallbacks
    {
        // ---- Per-crucial-node transient flow state (replaces CrucialGraphNode.CurrFlowState) ----
        //
        // Indexed by crucialIdx (0..graphState.NodeCount). Cleared to Empty at the top of each
        // test via Array.Clear — NodeCount is small (tens), so the clear is cheap.

        [HideInInspector] public FlowState[] NodeFlow;

        // ---- Per-crucial-node transient P↔N inversion (replaces CrucialGraphNode.TempTransformedType) ----
        //
        // Indexed by crucialIdx. Only meaningful for cells whose CellType is NTransistor or
        // PTransistor; reads for other types are benign (return CellType.NONE).

        [HideInInspector] public CellType[] NodeTempTransform;

        // ---- Per-crucial-node input values for the current test (replaces per-edge GetTestValBySubType) ----
        //
        // Populated once per test in ProcessPreparingTest by walking Input crucial nodes and
        // looking up their expected value from TestData. Read by DepthStepSystem at Input-origin
        // edges. Non-Input entries are unused.

        [HideInInspector] public FlowState[] InputFlowByNode;

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

        [HideInInspector] public FlowState[] CellFlow;
        [HideInInspector] public int[] CellFlowStamps;
        [HideInInspector] public int CurrentFlowStamp;

        // ---- Per-cell temp-transform (replaces GridCell.TempTransformation writes) ----
        //
        // Shares CurrentFlowStamp with CellFlow but has its OWN per-cell stamp array. This
        // matters: a gate-inversion edge writes temp-transform on a below-cell that may not
        // be on any path, so it shouldn't validate that cell's flow stamp. With separate
        // stamp arrays, writing temp-transform only validates temp-transform reads.

        [HideInInspector] public CellType[] CellTempTransform;
        [HideInInspector] public int[] CellTempTransformStamps;

        // ---- Output flow buffer (pooled, reused across all rows) ----
        //
        // Entry i holds the flow observed on the i-th Output crucial node for the most recent
        // row, in the order Output nodes appear in graphState.CrucialNodes. Sized once on
        // Simulate-mode entry and never resized — the output set is a property of the level,
        // not the row.

        [HideInInspector] public FlowState[] OutputFlowBuffer;
        [HideInInspector] public int OutputCount;

        // ---- Per-test flag set by DepthStepSystem, consumed by ProcessResolvingTest ----
        //
        // Mirrors SimulateRunState.IsUnstable. Duplicated here because DepthStepSystem already
        // has SimulateRunScratch in its permissions and this avoids broadening to RunState just
        // to flip one bool.

        [HideInInspector] public bool IsUnstable;

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
        public static void EnsureCapacity(SimulateRunScratch scratch, int nodeCount, int cellCount)
        {
            EnsureArray(ref scratch.NodeFlow, nodeCount, nameof(scratch.NodeFlow));
            EnsureArray(ref scratch.NodeTempTransform, nodeCount, nameof(scratch.NodeTempTransform));
            EnsureArray(ref scratch.InputFlowByNode, nodeCount, nameof(scratch.InputFlowByNode));

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

        // Clears per-node transient state for a new test. Small O(NodeCount) pass — NodeCount
        // is tens at most.
        public static void ClearNodeTransients(SimulateRunScratch scratch, int nodeCount)
        {
            if (scratch.NodeFlow != null) { Array.Clear(scratch.NodeFlow, 0, nodeCount); }
            if (scratch.NodeTempTransform != null) { Array.Clear(scratch.NodeTempTransform, 0, nodeCount); }
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

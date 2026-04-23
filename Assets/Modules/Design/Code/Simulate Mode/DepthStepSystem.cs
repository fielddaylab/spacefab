using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Per-depth flow-propagation worker. Only acts on frames where SimulateModeSystem flagged
    /// PaintDepthThisFrame = true and the phase is Propagating. Walks the edges in OrderedEdges
    /// at runState.CurrentDepth, computes flow per edge (diode gating, gate-above inversion,
    /// cycle handling), and writes FlowState / TempTransformation onto GridCells along each
    /// edge's path. Does not tick time or manage phase.
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
                    .ReadWriteShared<SimulateGraphState>()
                    .ReadWriteShared<GridStackState>()
                    .ReadShared<SimulateUIState>()
            );
        }

        // Paints the current depth's edges when signalled by SimulateModeSystem.
        static private void ProcessWork(float deltaTime)
        {
            // TODO: Find.State for runState, graphState, gridStackState.
            // TODO: if !runState.PaintDepthThisFrame → return.
            // TODO: if runState.Phase != SimulatePhase.Propagating → return (defensive).
            //
            // Iterate graphState.OrderedEdges[0..graphState.EdgeCount] and pick the subset where
            // edge.EvalDepth == runState.CurrentDepth. Because OrderedEdges is depth-sorted
            // (bucket-sorted in SimulateGraphUtility.Build Pass 3), edges at the target depth
            // form a contiguous slice — once we pass it we can break early.
            //
            // For each matching edge:
            //   - Port per-edge flow / diode / gate-above logic from EvaluationMgr.VisualFeedbackRoutine.
            //   - Walk path cells via graphState.PathPool[edge.PathStart .. edge.PathStart + edge.PathLength)
            //     and write FlowState into each cell via GridStackUtility.SetCellDirect.
            //   - Track per-node CurrFlowState / TempTransformedType (these are fields to add on
            //     CrucialNode when propagation lands — not on it yet; the struct currently holds
            //     only identity + depth).
            //   - If edge.CycleDetected is true, mark path cells unstable rather than honoring flow.
            //   - Set runState.IsUnstable = true if any convergence mismatch or cycle is encountered.
        }
    }
}

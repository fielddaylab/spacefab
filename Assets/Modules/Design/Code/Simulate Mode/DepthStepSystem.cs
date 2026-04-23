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
            // TODO: walk graphState.OrderedEdges where EvalDepth == runState.CurrentDepth;
            //       port per-edge flow / diode / gate-above logic from EvaluationMgr.VisualFeedbackRoutine
            //       inner loop. Write FlowState into each path cell via GridStackUtility.SetCellDirect.
            //       Update CurrFlowState / TempTransformedType on matching CrucialNodes.
            //       Set runState.IsUnstable = true if any convergence mismatch or cycle detected.
        }
    }
}

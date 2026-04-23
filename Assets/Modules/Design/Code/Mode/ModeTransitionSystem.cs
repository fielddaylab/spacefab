using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Facilitates transitioning between Design minigame modes (Tool vs. Simulate). Also owns the
    /// Simulate-mode entry/exit wiring: on Tool → Simulate, eagerly builds the evaluation graph
    /// and the eval-table UI and sizes RowVerdicts; on Simulate → Tool, invalidates the cached
    /// graph so a fresh Simulate entry rebuilds against the possibly-edited grid.
    /// Runs on Update at order 0, no category mask. Currently a stub.
    /// </summary>
    public class ModeTransitionSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<ModeTransitionState>()
                    .ReadWriteShared<SimulateRunState>()
                    .ReadWriteShared<SimulateGraphState>()
                    .ReadWriteShared<SimulateGraphBuildScratch>()
                    .ReadShared<GridStackState>()
                    .ReadWriteShared<SimulateUIState>()
            );
        }

        // TODO: implement mode transition logic.
        static private void ProcessWork(float deltaTime)
        {
            // On Tool → Simulate (eager build — graph is ready when player lands in Simulate):
            //   TODO: SimulateUIUtility.BuildTable(uiState, levelData.GetTestSuite()) if !uiState.TableBuilt.
            //   TODO: runState.RowVerdicts = new TestRowVerdict[suite.Tests.Length]   // defaults to Untested.
            //   TODO: Find.State<GridStackState>() + Find.State<SimulateGraphBuildScratch>().
            //         SimulateGraphUtility.Build(graphState, scratch, gridStackState).
            //         After Build: graphState.IsBuilt == true, ready for PreparingTest.
            //   TODO: runState.Phase = SimulatePhase.Idle.
            //   TODO: GameLoop.SuspendUpdates(UpdateMasks.ToolModeMask);
            //         GameLoop.ResumeUpdates(UpdateMasks.SimulateModeMask).
            //
            // On Simulate → Tool (entered via Cancelling, or eventually via explicit dismiss-then-exit):
            //   TODO: SimulateGraphUtility.Clear(graphState)  — keeps arrays, resets counts + IsBuilt.
            //         Scratch is NOT cleared (its arrays survive across sessions for zero-GC reuse).
            //   TODO: GameLoop.SuspendUpdates(UpdateMasks.SimulateModeMask);
            //         GameLoop.ResumeUpdates(UpdateMasks.ToolModeMask).
        }
    }
}

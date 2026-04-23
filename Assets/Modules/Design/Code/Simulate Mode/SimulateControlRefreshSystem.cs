using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Clears SimulateRunState's one-frame request flags (Play / Pause / Resume / Restart* / Cancel /
    /// Dismiss) and the PaintDepthThisFrame scratch flag at end of frame. Every Update-phase consumer
    /// gets one-frame visibility before they are wiped here.
    /// Runs on LateUpdate at order 100 under SimulateModeMask.
    /// </summary>
    public class SimulateControlRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateRunState>()
            );
        }

        // Clears every one-frame flag on SimulateRunState.
        static private void ProcessWork(float deltaTime)
        {
            SimulateRunState runState = Find.State<SimulateRunState>();
            runState.PlayFullSuiteRequested = false;
            runState.PlaySingleTestRequested = false;
            runState.PauseRequested = false;
            runState.ResumeRequested = false;
            runState.RestartTestRequested = false;
            runState.RestartSuiteRequested = false;
            runState.CancelRequested = false;
            runState.DismissResultsRequested = false;
            runState.PaintDepthThisFrame = false;
        }
    }
}

using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Clears SimulateRunState's one-frame request flags (Play / Pause / Resume / Restart* / Cancel /
    /// Dismiss) and the PaintDepthThisFrame scratch flag. Runs on Update at order 100 under
    /// SimulateModeMask, after every Update-phase consumer (ModeTransitionSystem at order 0,
    /// SimulateModeSystem at order 1, DepthStepSystem at order 2) has had a chance to read them.
    ///
    /// The order matters: Unity UI button clicks fire AFTER Update phase. If clearing
    /// happened in LateUpdate (or anywhere between the click and the next Update), a flag set by
    /// a click in frame N would be wiped before frame N+1's SimulateModeSystem could read it. By
    /// clearing within Update — before the click happens this frame — flags set by the click
    /// survive into the next frame's Update consumers and get processed exactly once.
    /// </summary>
    public class SimulateControlRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 100, UpdateMasks.SimulateModeMask),
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
            runState.PlayCurrentToggleComboRequested = false;
            runState.PaintDepthThisFrame = false;
        }
    }
}

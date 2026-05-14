using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Repaints the suite-level SuiteRunButton's icon (Play / Pause / Resume) when SimulateUIState
    /// flags the suite buttons dirty. Propagating shows Pause; Paused shows Resume; everything
    /// else shows Play. Runs under DesignMask alongside SuiteRunRowButtonRefreshSystem so the
    /// initial paint after BuildTable lands while still in Tool mode and stays correct across
    /// Tool↔Simulate transitions. Does NOT clear SuiteButtonsNeedRefreshing — the secondary
    /// refresh system runs after this and clears it once both systems have observed the flag.
    /// </summary>
    public class SuiteRunButtonRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 2, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadShared<SimulateRunState>()
                    .ReadWriteShared<SimulateUIState>()
            );
        }

        // Reads Phase and assigns the matching sprite to SuiteRunButton.Icon. Skips when the
        // table isn't built yet or the inspector ref hasn't been wired.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out SimulateRunState runState, out SimulateUIState uiState);

            if (!uiState.SuiteButtonsNeedRefreshing) { return; }
            if (!uiState.TableBuilt) { return; }
            if (uiState.SuiteRunButton == null || uiState.SuiteRunButton.Icon == null) { return; }

            var suiteDB = Find.GlobalAsset<SuiteVisualsDB>();

            SuiteRunButtonState state = SuiteRunButtonState.Play;
            if (runState.Phase == SimulatePhase.Propagating) { state = SuiteRunButtonState.Pause; }
            else if (runState.Phase == SimulatePhase.Paused) { state = SuiteRunButtonState.Resume; }

            uiState.SuiteRunButton.Icon.sprite = SuiteVisualsDBUtility.LookupRunButtonSprite(suiteDB, state);
        }
    }
}

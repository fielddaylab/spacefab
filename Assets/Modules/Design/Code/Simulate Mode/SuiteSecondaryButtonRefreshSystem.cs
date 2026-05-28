using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Toggles visibility on the suite-level Restart and Cancel buttons based on SimulatePhase
    /// and verdict history. Restart-Suite follows CanAcceptRestartSuite (Propagating or Paused).
    /// Cancel-Suite is visible whenever the grid is currently showing simulation visuals — i.e.
    /// any phase other than Idle, OR Idle with at least one resolved verdict still on display
    /// (after Dismiss from SuiteComplete, the grid still shows the last run's flow). The
    /// verdict-based check works because WipeRunState clears RowVerdicts and is the only path
    /// that wipes the grid back to empty. Buttons are hidden (GameObject deactivated) rather
    /// than greyed out so the toolbar only shows controls the player can act on. Runs after
    /// SuiteRunButtonRefreshSystem and clears SuiteButtonsNeedRefreshing once both systems
    /// have observed it.
    /// </summary>
    public class SuiteSecondaryButtonRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 3, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadShared<SimulateRunState>()
                    .ReadWriteShared<SimulateUIState>()
                    .ReadShared<DesignMinigameState>()
            );
        }

        // Reads Phase, computes the two interactable flags, applies them, then clears the
        // suite-button dirty flag for the next frame.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out SimulateRunState runState, out SimulateUIState uiState);
            DesignMinigameState designState = Find.State<DesignMinigameState>();

            // Toggle-input mode hides the Restart + Cancel buttons (they make no sense for a
            // one-row instant run from Tool mode). Still clear the dirty flag so the suite-button
            // signal lifecycle stays consistent.
            if (designState != null && designState.UseToggleInputMode)
            {
                if (uiState.SuiteRestartButton != null && uiState.SuiteRestartButton.gameObject.activeSelf)
                {
                    uiState.SuiteRestartButton.gameObject.SetActive(false);
                }
                if (uiState.SuiteCancelButton != null && uiState.SuiteCancelButton.gameObject.activeSelf)
                {
                    uiState.SuiteCancelButton.gameObject.SetActive(false);
                }
                uiState.SuiteButtonsNeedRefreshing = false;
                return;
            }

            if (!uiState.SuiteButtonsNeedRefreshing) { return; }
            if (!uiState.TableBuilt) { return; }

            // Restart-Suite is only meaningful mid full-suite run — the player asked to run the
            // whole suite, so "restart" means "go back to row 0 and run them all again." During
            // a single-test run, the per-row Restart-Test affordance is the right control.
            bool restartLegal = SimulateControlUtility.CanAcceptRestartSuite(runState)
                             && runState.Scope == RunScope.FullSuite;

            // Cancel-Suite stays visible whenever the grid is showing sim visuals: any non-Idle
            // phase obviously qualifies; Idle with leftover verdicts means the player landed
            // here via Dismiss-from-SuiteComplete (or similar) and the last run's flow is still
            // painted on the grid. WipeRunState is the one path that drops both Phase to Idle
            // AND clears verdicts, so this predicate flips to false exactly when there's
            // nothing left to cancel.
            bool cancelLegal = runState.Phase != SimulatePhase.Idle
                            || HasAnyResolvedVerdict(runState.RowVerdicts);

            if (uiState.SuiteRestartButton != null) { uiState.SuiteRestartButton.gameObject.SetActive(restartLegal); }
            if (uiState.SuiteCancelButton != null) { uiState.SuiteCancelButton.gameObject.SetActive(cancelLegal); }

            uiState.SuiteButtonsNeedRefreshing = false;
        }

        // True if at least one row carries a non-Untested verdict — i.e. some test has resolved
        // since the last WipeRunState. Tolerates a null array (pre-Simulate-entry).
        static private bool HasAnyResolvedVerdict(TestRowVerdict[] verdicts)
        {
            if (verdicts == null) { return false; }
            for (int i = 0; i < verdicts.Length; i++)
            {
                if (verdicts[i] != TestRowVerdict.Untested) { return true; }
            }
            return false;
        }
    }
}

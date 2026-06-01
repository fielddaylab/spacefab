using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Drives the toggle-input mode's single Test button:
    ///   - Hides the button when DesignMinigameState.UseToggleInputMode is false (the classic
    ///     per-row + suite-Run buttons take over).
    ///   - Whenever InputToggleState.MatchDirty is raised (toggle clicked or seed re-ran),
    ///     recomputes which TestData row matches the current toggle combo and caches the index
    ///     on InputToggleState.LastMatchedRowIndex.
    ///   - Sets the button interactable iff a matching row exists AND SimulateRunState is in a
    ///     phase that can accept a Play (CanAcceptPlay).
    /// Runs on LateUpdate at order 4 (after the classic suite-button refresh trio) under DesignMask
    /// so it stays correct across Tool↔Simulate transitions.
    /// </summary>
    public class SuiteTestButtonRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 4, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadShared<DesignMinigameState>()
                    .ReadWriteShared<InputToggleState>()
                    .ReadShared<SimulateRunState>()
                    .ReadShared<SimulateUIState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out SimulateRunState runState,
                out SimulateUIState uiState,
                out PlayerProgressState progressState
                );
            DesignMinigameState designState = Find.State<DesignMinigameState>();
            InputToggleState toggleState = Find.State<InputToggleState>();

            if (!uiState.TableBuilt || uiState.SuiteTestButton == null) { return; }

            bool toggleMode = designState != null && designState.UseToggleInputMode;
            ApplyVisibility(uiState, toggleMode);
            if (!toggleMode) { return; }

            // Refresh the matched test-row index on demand.
            if (toggleState != null && toggleState.MatchDirty)
            {
                TestSuiteData suite = ResolveSuite(progressState, designState);
                toggleState.LastMatchedRowIndex = InputToggleUtility.FindMatchingTestRow(toggleState, suite);
                toggleState.MatchDirty = false;
            }

            // Interactable iff we have a row to run AND the phase machine will accept a Play.
            bool matched = toggleState != null && toggleState.LastMatchedRowIndex >= 0;
            bool canPlay = SimulateControlUtility.CanAcceptPlay(runState);
            uiState.SuiteTestButton.interactable = matched && canPlay;
        }

        // Sets gameObject active state only when it actually needs to change so the loop stays
        // cheap on a steady frame.
        static private void ApplyVisibility(SimulateUIState uiState, bool toggleMode)
        {
            if (uiState.SuiteTestButton.gameObject.activeSelf != toggleMode)
            {
                uiState.SuiteTestButton.gameObject.SetActive(toggleMode);
            }
        }

        // Pulls the TestSuiteData off the active Design level; returns null on a missing chain so
        // FindMatchingTestRow can short-circuit to -1.
        static private TestSuiteData ResolveSuite(PlayerProgressState progressState, DesignMinigameState designState)
        {
            if (progressState == null || designState == null) { return null; }
            LevelData levelData = DesignLevelUtility.GetActiveLevelData(progressState, designState);
            if (levelData == null) { return null; }
            return levelData.GetTestSuite();
        }
    }
}

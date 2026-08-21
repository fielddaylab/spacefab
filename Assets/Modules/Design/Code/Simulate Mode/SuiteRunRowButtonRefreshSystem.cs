using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Repaints SuiteRunRowButton icons (Play / Pause / Resume) when SimulateUIState flags
    /// the buttons dirty. Active row in Propagating shows Pause; active row in Paused shows
    /// Resume; everything else shows Play. Runs under DesignMask (not SimulateModeMask) so the
    /// initial Play-icon paint after BuildTable lands while still in Tool mode, and so icons
    /// stay correct on any Tool↔Simulate transition.
    /// </summary>
    public class SuiteRunRowButtonRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 0, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadShared<SimulateRunState>()
                    .ReadWriteShared<SimulateUIState>()
                    .ReadShared<DesignMinigameState>()
            );
        }

        // Walks every row when the dirty flag is raised and assigns its button icon based on
        // current Phase + CurrentRow. Clears the flag after repainting.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out SimulateRunState runState, out SimulateUIState uiState);
            DesignMinigameState designState = Find.State<DesignMinigameState>();

            // Toggle-input mode hides every per-row Run button — keep them inactive every frame
            // so a runtime mode flip via inspector takes effect without re-running BuildTable.
            if (designState != null && designState.UseToggleInputMode)
            {
                //if (uiState.Rows != null)
                //{
                //    for (int r = 0; r < uiState.Rows.Length; r++)
                //    {
                //        SuiteRow row = uiState.Rows[r];
                //        if (row != null && row.RunButton != null && row.RunButton.gameObject.activeSelf)
                //        {
                //            row.RunButton.gameObject.SetActive(false);
                //        }
                //    }
                //}
                uiState.RunButtonsNeedRefreshing = false;
                return;
            }

            if (!uiState.RunButtonsNeedRefreshing) { return; }
            if (!uiState.TableBuilt || uiState.Rows == null) { return; }

            var suiteDB = Find.GlobalAsset<SuiteVisualsDB>();

            for (int row = 0; row < uiState.Rows.Length; row++)
            {
                SuiteRunButtonState state = SuiteRunButtonState.Play;
                if (runState.CurrentRow == row)
                {
                    if (runState.Phase == SimulatePhase.Propagating) { state = SuiteRunButtonState.Pause; }
                    else if (runState.Phase == SimulatePhase.Paused) { state = SuiteRunButtonState.Resume; }
                }

                //uiState.Rows[row].RunButton.Icon.sprite = SuiteVisualsDBUtility.LookupRunButtonSprite(suiteDB, state);
            }

            uiState.RunButtonsNeedRefreshing = false;
        }
    }
}

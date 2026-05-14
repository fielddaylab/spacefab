using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Repaints VerdictVisualizer icons (Correct / Incorrect / Hidden) when SimulateUIState
    /// flags the verdict visuals dirty. Reads uiState.CellVerdicts[row][col] and applies the
    /// matching sprite (or disables the icon for Hidden) to uiState.Rows[row].Verdicts[col].
    /// Runs under DesignMask so verdicts stay correct across Tool↔Simulate transitions and the
    /// initial Hidden paint after BuildTable lands while still in Tool mode.
    /// </summary>
    public class VerdictVisualizerRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 1, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateUIState>()
            );
        }

        // Walks every row × col when the dirty flag is raised and applies the verdict sprite
        // (or hides the icon) per CellVerdicts. Clears the flag after repainting.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out SimulateUIState uiState);

            if (!uiState.VerdictsNeedRefreshing) { return; }
            if (!uiState.TableBuilt || uiState.Rows == null || uiState.CellVerdicts == null) { return; }

            var suiteDB = Find.GlobalAsset<SuiteVisualsDB>();

            for (int row = 0; row < uiState.Rows.Length; row++)
            {
                CellVerdict[] verdicts = uiState.CellVerdicts[row];
                VerdictVisualizer[] visualizers = uiState.Rows[row].Verdicts;
                if (verdicts == null || visualizers == null) { continue; }

                int colCount = visualizers.Length;
                if (verdicts.Length < colCount) { colCount = verdicts.Length; }

                for (int col = 0; col < colCount; col++)
                {
                    VerdictVisualizer viz = visualizers[col];
                    if (viz == null || viz.Icon == null) { continue; }

                    CellVerdict state = verdicts[col];
                    if (state == CellVerdict.Hidden)
                    {
                        viz.Icon.enabled = false;
                    }
                    else
                    {
                        viz.Icon.enabled = true;
                        viz.Icon.sprite = SuiteVisualsDBUtility.LookupVerdictSprite(suiteDB, state);
                    }
                }
            }

            uiState.VerdictsNeedRefreshing = false;
        }
    }
}

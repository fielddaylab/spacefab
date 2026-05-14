using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Processes result-display requests for Simulate mode.
    ///
    /// Runs at Update order 2, after SimulateModeSystem (order 1) has written
    /// ResultsPanelVisible on SimulateUIState. Gated on SimulateModeMask to match
    /// SimulateModeSystem's own mask.
    ///
    /// Detects the rising edge of ResultsPanelVisible (false -> true) and calls
    /// ShowResults once per display request. allCorrect is derived from
    /// SimulateRunState.RowVerdicts — the same source SimulateModeSystem used when
    /// it decided to show the panel — so no extra flag field is required on any state.
    /// </summary>
    public class ResultSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 2, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<ResultState>()
                    .ReadShared<SimulateUIState>()
                    .ReadShared<SimulateRunState>()
            );
        }

        static private bool s_wasVisible = false;

        static private void ProcessWork(float deltaTime)
        {
            Find.State(out ResultState resultState, out SimulateUIState uiState, out SimulateRunState runState);

            bool isVisible = uiState.ResultsPanelVisible;

            // Rising edge only: panel just became visible this frame.
            if (isVisible && !s_wasVisible)
            {
                Debug.Log($"ResultSystem: Detected rising edge of ResultsPanelVisible; processing show request.");
                bool allCorrect = ResultStateUtility.IsAllCorrect(runState);
                ResultStateUtility.ShowResults(resultState, allCorrect);
            }

            s_wasVisible = isVisible;
        }

        // Mirrors SimulateModeSystem.IsAllCorrect. For a SingleTest run RowVerdicts has one
        // populated slot (CurrentRow); the rest remain at their default. IsAllCorrect returns
        // true only if every entry is Correct, so a single-test pass still works correctly as
        // long as ClearAllVerdicts initialises unused slots to Correct (or the suite length
        // is 1). If your default is Incorrect/Unstable, scope the loop to [0..CurrentRow]
        // for SingleTest runs instead.
        // static private bool IsAllCorrect(SimulateRunState runState)
        // {
        //     TestRowVerdict[] verdicts = runState.RowVerdicts;
        //     for (int i = 0; i < verdicts.Length; i++)
        //     {
        //         if (verdicts[i] != TestRowVerdict.Correct) { return false; }
        //     }
        //     return true;
        // }
    }
}
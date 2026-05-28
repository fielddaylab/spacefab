using FieldDay;
using UnityEngine.Scripting;

namespace SpaceFab.Design
{
    /// <summary>
    /// Static event-bridge for the Design minigame. Registers handlers at boot that translate
    /// game events into mutations on Design-owned state. Currently handles FoundValidSolution
    /// resets: any grid edit or test re-run invalidates the "solved" flag, which only flips
    /// back to true when a full-suite run completes with every verdict Correct (set directly
    /// by SimulateModeSystem.ProcessResolvingTest).
    /// </summary>
    [Preserve]
    public static class DesignTriggers
    {
        [InvokeOnBoot]
        static private void Init()
        {
            SpacefabGame.Events.Register(GameEvents.DesignGridModified, HandleGridModified);
            SpacefabGame.Events.Register(GameEvents.DesignSimPlayStarted, HandleSimPlayStarted);
        }

        // Player edited the grid; the previously-validated solution may no longer hold. In
        // toggle-input mode, the grid edit also invalidates every prior verdict (the player's
        // freshly-edited circuit hasn't been tested under the current toggles yet). Input cells
        // are immutable per-level, so the toggle entry set itself doesn't need to change.
        static private void HandleGridModified()
        {
            ClearFoundValidSolution();

            DesignMinigameState designState = Find.State<DesignMinigameState>();
            if (designState == null || !designState.UseToggleInputMode) { return; }

            SimulateRunState runState = Find.State<SimulateRunState>();
            SimulateUIState uiState = Find.State<SimulateUIState>();

            if (runState != null) { SimulateControlUtility.ClearAllVerdicts(runState); }
            if (uiState != null) { SimulateUIUtility.HideAllRowVerdicts(uiState); }
        }

        // A test (single or full-suite) just started. Until it completes successfully, treat
        // the design as unproven. Catches the case where the player runs a single test after
        // a passing full-suite run — that test could fail and invalidate the prior result.
        //
        // In toggle-input mode this clearing is suppressed: a single-test click is the normal
        // case, and FoundValidSolution is only set when *every* row has passed. Clearing on every
        // click would break the all-passed test that ProcessResolvingTest runs after each row.
        static private void HandleSimPlayStarted()
        {
            DesignMinigameState designState = Find.State<DesignMinigameState>();
            if (designState != null && designState.UseToggleInputMode) { return; }
            ClearFoundValidSolution();
        }

        // Both reset paths funnel here. DesignMinigameState may not be registered yet during
        // very early boot frames (or after minigame teardown); guard against that rather than
        // tying handler registration to lifetime.
        static private void ClearFoundValidSolution()
        {
            DesignMinigameState designState = Find.State<DesignMinigameState>();
            if (designState == null) { return; }
            designState.ClearFoundValidSolution();
        }
    }
}

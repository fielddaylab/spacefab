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

        // Player edited the grid; the previously-validated solution may no longer hold.
        static private void HandleGridModified()
        {
            ClearFoundValidSolution();
        }

        // A test (single or full-suite) just started. Until it completes successfully, treat
        // the design as unproven. Catches the case where the player runs a single test after
        // a passing full-suite run — that test could fail and invalidate the prior result.
        static private void HandleSimPlayStarted()
        {
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

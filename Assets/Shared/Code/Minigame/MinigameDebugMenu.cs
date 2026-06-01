using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;

namespace SpaceFab
{
    /// <summary>
    /// Development-only debug menu for the active minigame. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot; compiled out of release builds since the attribute is
    /// Conditional). Acts on whichever minigame the MinigameStateInterfacer currently points
    /// at, and the button disables itself when no minigame state is wired up.
    /// </summary>
    public static class MinigameDebugMenu
    {
        // Contributes Minigames -> SetSolved. The Minigames root merges with the JumpTo and Design
        // submenus contributed by other factories.
        [DebugMenuFactory]
        private static DMInfo CreateMinigameDebugMenu()
        {
            DMInfo menu = new DMInfo("Minigames", 1);
            menu.AddButton("SetSolved", DebugMarkValidSolution, HasActiveMinigameState);
            return menu;
        }

        // True only when a minigame's runtime state is wired into the interfacer.
        private static bool HasActiveMinigameState()
        {
            return Game.SharedState.Has<MinigameStateInterfacer>()
                && Find.State<MinigameStateInterfacer>().MinigameState != null;
        }

        // Forces the current minigame's runtime FoundValidSolution flag on.
        private static void DebugMarkValidSolution()
        {
            if (!HasActiveMinigameState())
            {
                Log.Warn("[MinigameDebugMenu] Set HasValidSolution unavailable: no active minigame state");
                return;
            }

            MinigameStateInterfacer interfacer = Find.State<MinigameStateInterfacer>();
            interfacer.MinigameState.MarkFoundValidSolution();
            Log.Msg("[MinigameDebugMenu] Set {0}.FoundValidSolution = true", interfacer.Id);
        }
    }
}

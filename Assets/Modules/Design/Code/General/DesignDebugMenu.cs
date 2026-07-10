using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using SpaceFab.Save;

namespace SpaceFab.Design
{
    /// <summary>
    /// Development-only debug menu for the Design minigame. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot; compiled out of release builds since the attribute is Conditional).
    /// The skip button marks the active level solved and advances to the next level — or to
    /// overarching when already on the last level — reusing the same path the results-panel
    /// "Continue" button drives. Disables itself when the Design minigame isn't active.
    /// </summary>
    public static class DesignDebugMenu
    {
        // Contributes Minigames -> Design -> Skip to Next Level. The Minigames root merges with the
        // SetSolved button and JumpTo submenu contributed by other factories.
        [DebugMenuFactory]
        private static DMInfo CreateDesignDebugMenu()
        {
            DMInfo menu = new DMInfo("Minigames", 1);
            DMInfo designMenu = new DMInfo("Design", 1);
            designMenu.AddButton("Skip to Next Level", DebugSkipLevel, IsDesignActive);
            menu.AddSubmenu(designMenu);
            return menu;
        }

        // True only inside the Design minigame, with the states the skip action needs present.
        private static bool IsDesignActive()
        {
            return Game.SharedState.Has<DesignMinigameState>()
                && Game.SharedState.Has<MinigameSaveStates>()
                && Game.SharedState.Has<MinigameRequestExitState>()
                && Game.SharedState.Has<MinigameStateInterfacer>();
        }

        // Marks the current level solved, then advances out of it (next-level reload or, on the last
        // level, exit to overarching) via the shared continue path.
        private static void DebugSkipLevel()
        {
            if (!IsDesignActive())
            {
                Log.Warn("[DesignDebugMenu] Skip level unavailable: Design minigame not active");
                return;
            }

            Find.State(
                out DesignMinigameState designState,
                out MinigameSaveStates saveStates,
                out MinigameRequestExitState requestExitState
                );
            MinigameStateInterfacer interfacer = Find.State<MinigameStateInterfacer>();

            DesignLevelUtility.MarkActiveLevelSolved(saveStates.Design, designState);
            DesignLevelUtility.AdvanceFromActiveLevel(saveStates.Design, designState, requestExitState, interfacer);
            Log.Msg("[DesignDebugMenu] Skipped level {0}", designState.ActiveLevelIndex);
        }
    }
}

using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using SpaceFab.Materials;
using SpaceFab.Research;
using SpaceFab.UI;

namespace SpaceFab
{
    /// <summary>
    /// Development-only debug menu for material knowledge. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot; compiled out of release builds since the attribute is
    /// Conditional). Contributes Materials -> Unlock All Knowledge, which confirms every
    /// discoverable property for every registered material, folds the result into a live
    /// Research session, and unlocks each material's wiki page, putting account progress
    /// into the fully-researched state. Disables itself when the states it writes to are
    /// not present.
    /// </summary>
    public static class MaterialKnowledgeDebugMenu
    {
        // Contributes the Materials root.
        [DebugMenuFactory]
        private static DMInfo CreateMaterialKnowledgeDebugMenu()
        {
            DMInfo menu = new DMInfo("Materials", 1);
            menu.AddButton("Unlock All Knowledge", DebugUnlockAllKnowledge, CanUnlock);
            return menu;
        }

        // True only when the account-scoped progress state and the material registry needed to
        // enumerate every material are both present.
        private static bool CanUnlock()
        {
            return Game.SharedState.Has<PlayerProgressState>()
                && Find.GlobalAsset<MaterialOrderAsset>() != null;
        }

        // Confirms each material's discoverable properties, then unlocks each material's wiki
        // page. Mirrors the end state the Research confirm flow eventually produces, but
        // applied in bulk straight to PlayerProgressState.
        private static void DebugUnlockAllKnowledge()
        {
            if (!CanUnlock())
            {
                Log.Warn("[MaterialKnowledgeDebugMenu] Unlock All Knowledge unavailable: required state/asset not present");
                return;
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            MaterialOrderAsset materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            int materialCount = materialOrder.Count;

            // A material's discoverable properties are authored on its MaterialAsset, and those
            // assets only mount for in-game scenes - so the set is read from the snapshot baked
            // onto MaterialOrderAsset instead, which covers every material in the project.
            // A stale or absent snapshot silently unlocks less than it should, so say so.
            if (!materialOrder.HasBakedKnowledge)
            {
                Log.Warn("[MaterialKnowledgeDebugMenu] Baked material knowledge is missing or stale - run SpaceFab > Materials > Rebake Material Knowledge");
            }

            // 1. Confirm each material's discoverable properties. Properties the material
            // doesn't have are absent from the snapshot and stay unconfirmed.
            int unlockedCount = 0;
            for (int i = 0; i < materialCount; i++)
            {
                if (PlayerProgressUtility.ConfirmAll(progressState, materialOrder.GetId(i), materialOrder.GetBakedKnowledge(i)))
                {
                    unlockedCount++;
                }
            }

            // 2. Fold the new progress into the Research sandbox if the minigame is live.
            // Research reads its own in-session sandbox rather than PlayerProgressState, so
            // without this the tray gems and requirements panel keep showing pre-unlock
            // state until the minigame is re-entered. The merge raises the frame flag those
            // refresh systems are gated on; the hypothesis viewmodel reads confirmed state
            // too, so it gets invalidated alongside them.
            if (Game.SharedState.Has<ResearchMinigameState>())
            {
                ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
                if (ResearchStateUtility.MergeFromPlayerProgress(researchState, progressState)
                    && Game.SharedState.Has<HypothesisViewModelState>())
                {
                    HypothesisViewModelUtility.RequestRebuild(Find.State<HypothesisViewModelState>());
                }
            }

            // 3. Unlock each material's wiki page. Material wiki pages are authored with the
            // same asset name as the MaterialAsset, so the material id is the page id (the same
            // mapping the Research confirm bridge relies on). UnlockPage short-circuits on
            // duplicates, so re-running this is a no-op for already-unlocked pages.
            for (int i = 0; i < materialCount; i++)
            {
                WikiUtility.UnlockPage(progressState, materialOrder.GetId(i));
            }

            Log.Msg("[MaterialKnowledgeDebugMenu] Unlocked all knowledge for {0} materials ({1} gained new properties)", materialCount, unlockedCount);
        }
    }
}

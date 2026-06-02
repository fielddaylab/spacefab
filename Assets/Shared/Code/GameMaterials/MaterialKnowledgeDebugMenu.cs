using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using SpaceFab.Materials;
using SpaceFab.UI;

namespace SpaceFab
{
    /// <summary>
    /// Development-only debug menu for material knowledge. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot; compiled out of release builds since the attribute is
    /// Conditional). Contributes Materials -> Unlock All Knowledge, which confirms every
    /// persistent property for every registered material and unlocks each material's wiki
    /// page, putting account progress into the fully-researched state. Disables itself when
    /// the states it writes to aren't present.
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

        // Confirms every persistent property for every registered material, then unlocks each
        // material's wiki page. Mirrors the end state the Research confirm flow eventually
        // produces, but applied in bulk straight to PlayerProgressState.
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

            // Cache the persistent label set once. Static persistent labels ignore the context
            // material; dynamic labels (PDopantFor / NDopantFor) are parameterized by a context
            // material and so must be confirmed against every other material in turn.
            var labels = (MaterialPropertyLabel[]) Enum.GetValues(typeof(MaterialPropertyLabel));

            // 1. Confirm every persistent property on every material.
            for (int i = 0; i < materialCount; i++)
            {
                StringHash32 materialId = materialOrder.GetId(i);

                for (int l = 0; l < labels.Length; l++)
                {
                    MaterialPropertyLabel label = labels[l];
                    if (!MaterialPropertyLabelUtility.IsPersistent(label))
                    {
                        continue;
                    }

                    if (MaterialPropertyLabelUtility.IsDynamic(label))
                    {
                        // Dynamic label: confirm it against each registered context material.
                        for (int c = 0; c < materialCount; c++)
                        {
                            PlayerProgressUtility.Confirm(progressState, materialId, label, materialOrder.GetId(c));
                        }
                    }
                    else
                    {
                        // Static label: context is ignored.
                        PlayerProgressUtility.Confirm(progressState, materialId, label, StringHash32.Null);
                    }
                }
            }

            // 2. Unlock each material's wiki page. Material wiki pages are authored with the
            // same asset name as the MaterialAsset, so the material id is the page id (the same
            // mapping the Research confirm bridge relies on). UnlockPage short-circuits on
            // duplicates, so re-running this is a no-op for already-unlocked pages.
            for (int i = 0; i < materialCount; i++)
            {
                WikiUtility.UnlockPage(progressState, materialOrder.GetId(i));
            }

            Log.Msg("[MaterialKnowledgeDebugMenu] Unlocked all knowledge for {0} materials", materialCount);
        }
    }
}

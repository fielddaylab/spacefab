using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using SpaceFab.Materials;
using SpaceFab.Research;
using SpaceFab.UI;

namespace SpaceFab {
    /// <summary>
    /// Development-only debug menu for material knowledge. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot; compiled out of release builds since the attribute is
    /// Conditional). Contributes the Materials root: Unlock All Knowledge, plus one submenu per
    /// material holding a button for each property discoverable on it. Every unlock writes
    /// account progress, unlocks the material's wiki page, and folds the result into a live
    /// Research session.
    ///
    /// The hierarchy is built once at boot from MaterialOrderAsset's baked snapshot - the
    /// MaterialAssets themselves only mount for in-game scenes, so their names and property
    /// sets aren't reachable this early. Global asset packs mount in GameLoop.Start, ahead of
    /// the frame-start queue the menu factories run on, so the ordering asset is available
    /// here; if it somehow isn't, only Unlock All Knowledge is contributed.
    /// </summary>
    public static class MaterialKnowledgeDebugMenu {
        // Contributes the Materials root.
        [DebugMenuFactory]
        private static DMInfo CreateMaterialKnowledgeDebugMenu()
        {
            int materialCount = Game.Assets.TryGetGlobal(out MaterialOrderAsset materialOrder) ? materialOrder.Count : 0;

            DMInfo menu = new DMInfo("Materials", materialCount + 2);
            menu.AddButton("Unlock All Knowledge", DebugUnlockAllKnowledge, CanUnlock);

            if (materialCount > 0)
            {
                menu.AddDivider();
                for (int i = 0; i < materialCount; i++)
                {
                    menu.AddSubmenu(BuildMaterialMenu(materialOrder, i));
                }
            }

            return menu;
        }

        // Builds one material's submenu: an unlock-everything button followed by one button per
        // property discoverable on that material, read off the baked snapshot. A material with
        // an empty snapshot still gets a folder, so a missing bake reads as empty rather than
        // as a material that doesn't exist.
        private static DMInfo BuildMaterialMenu(MaterialOrderAsset materialOrder, int materialIndex)
        {
            StringHash32 materialId = materialOrder.GetId(materialIndex);

            List<KeyValuePair<MaterialPropertyLabel, StringHash32>> properties = new List<KeyValuePair<MaterialPropertyLabel, StringHash32>>();
            MaterialPropertyRecordUtility.EnumerateConfirmed(materialOrder.GetBakedKnowledge(materialIndex), properties);

            DMInfo menu = new DMInfo(materialOrder.GetBakedName(materialIndex), properties.Count + 2);
            menu.AddButton("Unlock All", () => DebugUnlockMaterial(materialOrder, materialIndex), () => CanUnlockMaterial(materialOrder, materialIndex));
            menu.AddDivider();

            for (int i = 0; i < properties.Count; i++)
            {
                // Capture per-iteration so each button closes over its own property.
                MaterialPropertyLabel label = properties[i].Key;
                StringHash32 contextMaterialId = properties[i].Value;
                menu.AddButton(
                    FormatProperty(materialOrder, label, contextMaterialId),
                    () => DebugUnlockProperty(materialId, label, contextMaterialId),
                    () => CanUnlockProperty(materialId, label, contextMaterialId)
                );
            }

            return menu;
        }

        // Button label for one discoverable property. Dynamic labels are parameterized by a
        // context material, so "PDopantFor" on its own would be ambiguous across contexts.
        private static string FormatProperty(MaterialOrderAsset materialOrder, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (contextMaterialId.IsEmpty || !materialOrder.TryGetIndex(contextMaterialId, out int contextIndex))
            {
                return label.ToString();
            }

            return label.ToString() + " " + materialOrder.GetBakedName(contextIndex);
        }

        // True only when the account-scoped progress state and the material registry needed to
        // enumerate every material are both present. Runs every frame the menu is open, so it
        // goes through TryGetGlobal, which reports a missing asset instead of asserting.
        private static bool CanUnlock()
        {
            return Game.SharedState.Has<PlayerProgressState>()
                && Game.Assets.TryGetGlobal(out MaterialOrderAsset _);
        }

        // Gates a single-property button. Going dead once the property is confirmed turns the
        // submenu into a readout of what the account already knows. Runs every frame the menu
        // is open, so it stays a dictionary lookup plus a mask test.
        private static bool CanUnlockProperty(StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            return CanUnlock()
                && !PlayerProgressUtility.HasConfirmed(Find.State<PlayerProgressState>(), materialId, label, contextMaterialId);
        }

        // Gates a material's Unlock All button: live only while the baked snapshot still holds
        // something the account hasn't confirmed.
        private static bool CanUnlockMaterial(MaterialOrderAsset materialOrder, int materialIndex)
        {
            if (!CanUnlock())
            {
                return false;
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            progressState.MaterialProperties.TryGetValue(materialOrder.GetId(materialIndex), out var confirmed);

            MaterialPropertyRecord merged = confirmed;
            MaterialPropertyRecordUtility.Merge(ref merged, materialOrder.GetBakedKnowledge(materialIndex));
            return !MaterialPropertyRecordUtility.AreEqual(confirmed, merged);
        }

        // Confirms every material's discoverable properties and unlocks every wiki page.
        // Mirrors the end state the Research confirm flow eventually produces, but applied in
        // bulk straight to PlayerProgressState.
        private static void DebugUnlockAllKnowledge()
        {
            if (!CanUnlock())
            {
                Log.Warn("[MaterialKnowledgeDebugMenu] Unlock All Knowledge unavailable: required state/asset not present");
                return;
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            Game.Assets.TryGetGlobal(out MaterialOrderAsset materialOrder);
            int materialCount = materialOrder.Count;

            // A material's discoverable properties are authored on its MaterialAsset, and those
            // assets only mount for in-game scenes - so the set is read from the snapshot baked
            // onto MaterialOrderAsset instead, which covers every material in the project.
            // A stale or absent snapshot silently unlocks less than it should, so say so.
            if (!materialOrder.HasBakedKnowledge)
            {
                Log.Warn("[MaterialKnowledgeDebugMenu] Baked material knowledge is missing or stale - run SpaceFab > Materials > Rebake Material Knowledge");
            }

            // Properties the material doesn't have are absent from the snapshot and stay
            // unconfirmed. Wiki pages are authored with the same asset name as the
            // MaterialAsset, so the material id is the page id; UnlockPage short-circuits on
            // duplicates, so re-running this is a no-op for pages already unlocked.
            int unlockedCount = 0;
            for (int i = 0; i < materialCount; i++)
            {
                StringHash32 materialId = materialOrder.GetId(i);
                if (PlayerProgressUtility.ConfirmAll(progressState, materialId, materialOrder.GetBakedKnowledge(i)))
                {
                    unlockedCount++;
                }
                WikiUtility.UnlockPage(progressState, materialId);
            }

            SyncResearchSandbox(progressState);
            Log.Msg("[MaterialKnowledgeDebugMenu] Unlocked all knowledge for {0} materials ({1} gained new properties)", materialCount, unlockedCount);
        }

        // Confirms everything discoverable on a single material.
        private static void DebugUnlockMaterial(MaterialOrderAsset materialOrder, int materialIndex)
        {
            if (!CanUnlock())
            {
                Log.Warn("[MaterialKnowledgeDebugMenu] Unlock unavailable: required state/asset not present");
                return;
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            StringHash32 materialId = materialOrder.GetId(materialIndex);

            PlayerProgressUtility.ConfirmAll(progressState, materialId, materialOrder.GetBakedKnowledge(materialIndex));
            WikiUtility.UnlockPage(progressState, materialId);
            SyncResearchSandbox(progressState);

            Log.Msg("[MaterialKnowledgeDebugMenu] Unlocked all knowledge for '{0}'", materialOrder.GetBakedName(materialIndex));
        }

        // Confirms one discoverable property. Unlocks the material's wiki page alongside it,
        // the way ResearchPropertyConfirmBridge does on a real confirmation.
        private static void DebugUnlockProperty(StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (!CanUnlock())
            {
                Log.Warn("[MaterialKnowledgeDebugMenu] Unlock unavailable: required state/asset not present");
                return;
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();

            PlayerProgressUtility.Confirm(progressState, materialId, label, contextMaterialId);
            WikiUtility.UnlockPage(progressState, materialId);
            SyncResearchSandbox(progressState);

            Log.Msg("[MaterialKnowledgeDebugMenu] Confirmed '{0}' for material '{1}'", label, materialId.ToDebugString());
        }

        // Research reads its own in-session sandbox rather than PlayerProgressState, so
        // progress written from here stays invisible until it's folded back in. The merge
        // raises the frame flag the tray-rig and requirements-panel refreshes are gated on; the
        // hypothesis viewmodel reads confirmed state too, so it gets invalidated alongside them.
        private static void SyncResearchSandbox(PlayerProgressState progressState)
        {
            if (!Game.SharedState.Has<ResearchMinigameState>())
            {
                return;
            }

            ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
            if (ResearchStateUtility.MergeFromPlayerProgress(researchState, progressState)
                && Game.SharedState.Has<HypothesisViewModelState>())
            {
                HypothesisViewModelUtility.RequestRebuild(Find.State<HypothesisViewModelState>());
            }
        }
    }
}

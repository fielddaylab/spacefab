using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.Overarching;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public enum ChapterLoadPhase
    {
        Waiting,
        LoadingChapter,
        LoadingAvailableContracts,
        Completed
    }

    [Serializable]
    public struct ChapterLoadBundle
    {
        public SceneReference ChapterScene;
    }

    public class ChapterLoadState : SharedStateComponent
    {
        public ChapterLoadPhase Phase;
        public ChapterLoadBundle[] Chapters;

        public Routine LoadRoutine;
    }

    public static class ChapterLoadUtility
    {
        public static IEnumerator LoadCurrChapter(ChapterState chapterState, ChapterLoadState chapterLoadState)
        {
            if (Game.Scenes.IsLoaded(chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterScene))
            {
                chapterLoadState.Phase = ChapterLoadPhase.Completed;
                yield break;
            }

            // Unload prev chapter, if applicable
            if (chapterState.CurrChapterIndex > 0)
            {
                var prevChapterScene = chapterLoadState.Chapters[chapterState.CurrChapterIndex - 1].ChapterScene;
                if (Game.Scenes.IsLoaded(prevChapterScene))
                {
                    Game.Scenes.UnloadScene(prevChapterScene);

                    while (Game.Scenes.IsUnloading())
                    {
                        yield return null;
                    }
                }
            }

            // loaded until next chapter begins
            Game.Scenes.LoadPersistentScene(chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterScene);
            //chapterState.CurrChapterAssetPack = chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterAssetPack;
            //Game.Assets.LoadPackage(chapterState.CurrChapterAssetPack);

            while (Game.Scenes.IsLoading(chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterScene))
            {
                yield return null;
            }

            chapterLoadState.Phase = ChapterLoadPhase.Completed;

            // chapterState.CurrChapterDef = Find.NamedAsset<ChapterDef>(chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterDefId);

            /*
            // loaded whenever in overarching scene
            chapterState.CurrAvailableContractAssetsPack = chapterState.CurrChapterDef.AvailableContracts;

            ChapterLoadUtility.LoadAvailableContracts(chapterState);

            if (chapterState.LastSelectedContractIndex != -1)
            {
                ChapterLoadUtility.OnCurrContractKnown(chapterState, progressState, chapterState.LastSelectedContractIndex);
            }
            */
        }

        public static IEnumerator LoadCurrAvailableContracts(ChapterState chapterState, ChapterLoadState chapterLoadState, AvailableContractsLookup lookup)
        {
            var availableContractsScene = lookup.Entries[chapterState.CurrChapterIndex].Scene;
            Game.Scenes.LoadPersistentScene(availableContractsScene);

            while (Game.Scenes.IsLoading(availableContractsScene))
            {
                yield return null;
            }

            chapterLoadState.Phase = ChapterLoadPhase.Completed;

            // chapterState.CurrChapterDef = Find.NamedAsset<ChapterDef>(chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterDefId);

            /*
            // loaded whenever in overarching scene
            chapterState.CurrAvailableContractAssetsPack = chapterState.CurrChapterDef.AvailableContracts;

            ChapterLoadUtility.LoadAvailableContracts(chapterState);

            if (chapterState.LastSelectedContractIndex != -1)
            {
                ChapterLoadUtility.OnCurrContractKnown(chapterState, progressState, chapterState.LastSelectedContractIndex);
            }
            */
        }

        public static void LoadAvailableContracts(ChapterState chapterState)
        {
            // loaded whenever entering overarching scene
            Game.Assets.LoadPackage(chapterState.CurrAvailableContractAssetsPack);
            chapterState.CurrAvailableContractsBundle = Find.NamedAsset<ContractsBundle>(chapterState.CurrChapterDef.AvailableContractsBundleId);
        }

        public static void OnCurrContractKnown(ChapterState chapterState, PlayerProgressState progressState, int selectedContractIndex)
        {
            chapterState.CurrSelectedContractAssetPack = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].ContractAssets();
            Game.Assets.LoadPackage(chapterState.CurrSelectedContractAssetPack);

            // Unpack further
            StringHash32 assetsWrapperId = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].ContractAssetsWrapperId;
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(assetsWrapperId);
            // design level starts as initial config by default
            var minigameSaveState = Find.State<MinigameSaveStates>();
            minigameSaveState.Design.GridStack = new GridStack();
            GridStackUtility.LoadConfig(ref minigameSaveState.Design.GridStack, contractAssets.DesignLevelData.GetGridConfig());

            progressState.LastSelectedContract = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectedContractIndex].AssetId;
        }
    }
}
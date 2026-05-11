using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
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

            while (Game.Scenes.IsLoading(chapterLoadState.Chapters[chapterState.CurrChapterIndex].ChapterScene)
                || Game.Scenes.IsLoadingAnyScene())
            {
                yield return null;
            }

            chapterLoadState.Phase = ChapterLoadPhase.Completed;
        }

        public static IEnumerator LoadCurrAvailableContracts(ChapterState chapterState, ChapterLoadState chapterLoadState, AvailableContractsLookup lookup)
        {
            yield return ContractsLookupUtility.LoadAvailableContractsAtChapter(lookup, chapterState, chapterState.CurrChapterIndex);
            chapterState.CurrChapterDef = Find.NamedAsset<ChapterDef>(lookup.Entries[chapterState.CurrChapterIndex].ChapterId);

            chapterLoadState.Phase = ChapterLoadPhase.Completed;
        }

        /*
        public static void LoadAvailableContracts(ChapterState chapterState)
        {
            // loaded whenever entering overarching scene
            Game.Assets.LoadPackage(chapterState.CurrAvailableContractAssetsPack);
            chapterState.CurrAvailableContractsBundle = Find.NamedAsset<ContractsBundle>(chapterState.CurrChapterDef.AvailableContractsBundleId);
        }
        */
    }
}
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [Serializable]
    public struct AvailableContractLookupEntry {
        public SceneReference Scene;
        [AssetName(typeof(ContractsBundle))] [SerializeField] public StringHash32 BundleId;
        [AssetName(typeof(ChapterDef))] [SerializeField] public StringHash32 ChapterId;
    }


    public class AvailableContractsLookup : SharedStateComponent
    {
        public AvailableContractLookupEntry[] Entries;
    }

    public static partial class ContractsLookupUtility
    {
        public static IEnumerator LoadAvailableContractsAtChapter(AvailableContractsLookup lookup, ChapterState chapterState, int chapterIndex)
        {
            var availableContractsScene = lookup.Entries[chapterIndex].Scene;
            if (Game.Scenes.IsLoaded(availableContractsScene))
            {
                yield break;
            }

            Game.Scenes.LoadPersistentScene(availableContractsScene);

            while (Game.Scenes.IsLoading(availableContractsScene) || Game.Scenes.IsLoadingAnyScene())
            {
                yield return null;
            }

            chapterState.CurrAvailableContractsBundle = Find.NamedAsset<ContractsBundle>(lookup.Entries[chapterIndex].BundleId);
        }

        public static IEnumerator UnloadAvailableContractsAtChapter(AvailableContractsLookup lookup, ChapterState chapterState, int chapterIndex)
        {
            var availableContractsScene = lookup.Entries[chapterIndex].Scene;
            if (!Game.Scenes.IsLoaded(availableContractsScene))
            {
                yield break;
            }

            Game.Scenes.UnloadScene(availableContractsScene);

            while (Game.Scenes.IsUnloading())
            {
                yield return null;
            }

            chapterState.CurrAvailableContractsBundle = null;
        }
    }
}

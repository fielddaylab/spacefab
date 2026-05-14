using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using UnityEngine;

namespace SpaceFab.Supply
{
    [Serializable]
    public struct SupplyChainMapLookupEntry
    {
        public SceneReference Scene;
        [AssetName(typeof(SupplyChainMapData))] [SerializeField] public StringHash32 MapId;
    }

    public class SupplyChainMapLookup : SharedStateComponent, IRegistrationCallbacks
    {
        public SupplyChainMapLookupEntry[] Entries;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
        }
    }

    public static class SupplyChainMapLookupUtility
    {
        public static IEnumerator LoadChapterMap(SupplyChainMapLookup lookup, SupplyMinigameState supplyState, SupplyTransitionState transitionState, int chapterIndex)
        {
            var entry = lookup.Entries[chapterIndex];

            if (!Game.Scenes.IsLoaded(entry.Scene))
            {
                Game.Scenes.LoadPersistentScene(entry.Scene);

                while (Game.Scenes.IsLoading(entry.Scene) || Game.Scenes.IsLoadingAnyScene())
                {
                    yield return null;
                }
            }

            supplyState.CurrSupplyChainMap = Find.NamedAsset<SupplyChainMapData>(entry.MapId);
            transitionState.Phase = SupplyTransitionPhase.Completed;
        }

        public static IEnumerator UnloadChapterMap(SupplyChainMapLookup lookup, int chapterIndex)
        {
            var entry = lookup.Entries[chapterIndex];

            if (!Game.Scenes.IsLoaded(entry.Scene))
            {
                yield break;
            }

            Game.Scenes.UnloadScene(entry.Scene);

            while (Game.Scenes.IsUnloading())
            {
                yield return null;
            }
        }
    }
}

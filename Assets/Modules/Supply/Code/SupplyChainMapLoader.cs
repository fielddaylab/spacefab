using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.SharedState;
using ScriptableBake;
using System;
using System.Collections;
using UnityEngine;

namespace SpaceFab.Supply
{
    public class SupplyChainMapLoader : SharedStateComponent, IBaked
    {
        public SupplyChainMapData[] Entries;

        public Transform NodeRoot;
        public SupplyRouteNode[] Nodes;

#if UNITY_EDITOR

        int IBaked.Order { get { return 1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Nodes = NodeRoot.GetComponentsInChildren<SupplyRouteNode>(true);
            return true;
        }

#endif // UNITY_EDITOR
    }

    public static class SupplyChainMapLookupUtility
    {
        public static IEnumerator LoadChapterMap(SupplyChainMapLoader lookup, SupplyMinigameState supplyState, SupplyTransitionState transitionState, int chapterIndex)
        {
            var entry = lookup.Entries[chapterIndex];

            supplyState.CurrSupplyChainMap = entry;
            transitionState.Phase = SupplyTransitionPhase.Completed;
            yield return null;
        }

        public static IEnumerator UnloadChapterMap(SupplyChainMapLoader lookup, int chapterIndex)
        {
            var entry = lookup.Entries[chapterIndex];
            yield break;
        }
    }
}

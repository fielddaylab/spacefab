using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.SharedState;
using ScriptableBake;
using SpaceFab.Materials;
using SpaceFab.Research;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply
{
    public class SupplyChainMap : SharedStateComponent, IBaked, IScenePreload, ISceneLateInitialize
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

        void ISceneLateInitialize.LateInitialize() {
            foreach(var node in Nodes) {
                node.Position = node.transform.localPosition;
            }
        }

#endif // UNITY_EDITOR

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            for(int i = 0; i < Nodes.Length; i++) {
                var node = Nodes[i];
                node.Id = node.name;
                node.Index = i;

                if (node.Type != SupplyRouteNodeType.Home && node.InfoPopup != null) {
                    ResearchMaterialView matView = Find.NamedAsset<ResearchMaterialView>(node.MaterialType);
                    node.InfoPopup.OutputMaterialIcon.sprite = matView.SingleAtomSprite;
                }

                node.gameObject.SetActive(false);
                yield return null;
            }
        }
    }

    static public partial class SupplyRouteUtility {
        static public SupplyRouteNode GetNodeForIndex(int index) {
            Find.State(out SupplyChainMap loader);
            Assert.True(index >= 0 && index < loader.Nodes.Length, "Supply node index {0} out of range", index);
            return loader.Nodes[index];
        }

        static public SupplyRouteNode GetNodeForId(StringHash32 id) {
            if (id.IsEmpty) {
                return null;
            }

            Find.State(out SupplyChainMap loader);
            foreach(var node in loader.Nodes) {
                if (node.Id == id) {
                    return node;
                }
            }

            Assert.Fail("No supply node with id '{0}'", id);
            return null;
        }
    }

    public static class SupplyChainMapLookupUtility
    {
        public static IEnumerator LoadChapterMap(SupplyChainMap lookup, SupplyMinigameState supplyState, SupplyTransitionState transitionState, int chapterIndex)
        {
            var entry = lookup.Entries[chapterIndex];

            supplyState.CurrSupplyChainMap = entry;
            transitionState.Phase = SupplyTransitionPhase.Completed;
            yield return null;
        }

        public static IEnumerator UnloadChapterMap(SupplyChainMap lookup, int chapterIndex)
        {
            var entry = lookup.Entries[chapterIndex];
            yield break;
        }
    }
}

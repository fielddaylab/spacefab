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

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SpaceFab.Supply
{
    public class SupplyChainMap : SharedStateComponent, IBaked, IScenePreload
    {
        public SupplyChainMapData[] Entries;

        public Transform NodeRoot;
        public SupplyRouteNode[] Nodes;

        [NonSerialized] public SupplyRouteNode Home;

#if UNITY_EDITOR

        int IBaked.Order { get { return 1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Nodes = NodeRoot.GetComponentsInChildren<SupplyRouteNode>(true);
            return true;
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

                SupplyRouteUtility.InitializeTooltipReferences(node);
                node.gameObject.SetActive(false);
                yield return null;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SupplyChainMap))]
        private sealed class Inspector : Editor {
            public SupplyChainMapData MapData;

            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("-- EDITING --", EditorStyles.boldLabel);

                SupplyChainMap map = (SupplyChainMap)target;

                MapData = (SupplyChainMapData) EditorGUILayout.ObjectField("Edit Map", MapData, typeof(SupplyChainMapData), false);
                if (MapData != null) {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Load Data")) {
                        EditorImport(MapData, map.NodeRoot);
                    }
                    if (GUILayout.Button("Save Data")) {
                        EditorExport(map.NodeRoot, MapData);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        static private void EditorExport(Transform root, SupplyChainMapData target) {
            List<SupplyChainMapData.NodeData> nodes = new List<SupplyChainMapData.NodeData>();
            List<SupplyChainMapData.NodeOverride> overrides = new List<SupplyChainMapData.NodeOverride>();

            int childCount = root.childCount;
            for(int i = 0; i < childCount; i++) {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeSelf) {
                    continue;
                }

                if (!child.TryGetComponent(out SupplyRouteNode node)) {
                    continue;
                }

                Vector2 position = node.transform.localPosition;
                nodes.Add(new SupplyChainMapData.NodeData() {
                    Name = node.gameObject.name,
                    Position = position
                });

                // TODO: overrides
            }

            Baking.PrepareUndo(target, "Exporting map data");
            target.Positions = nodes.ToArray();
            target.Overrides = overrides.ToArray();

            Log.Msg("[SupplyChainMap] Exported map data ({0} nodes) to '{1}'", target.Positions.Length, target.name);
        }

        static private void EditorImport(SupplyChainMapData data, Transform root) {
            int childCount = root.childCount;
            for(int i = 0; i < childCount; i++) {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeSelf) {
                    continue;
                }

                if (!child.TryGetComponent(out SupplyRouteNode node)) {
                    continue;
                }

                Baking.PrepareUndo(child, "Importing map data");

                StringHash32 id = node.gameObject.name;
                bool activeState = false;
                foreach(var pos in data.Positions) {
                    if (pos.Name == id) {
                        child.localPosition = pos.Position;
                        activeState = true;
                        break;
                    }
                }

                child.gameObject.SetActive(activeState);
                Baking.SetDirty(child);
            }
        }
#endif // UNITY_EDITOR
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
}

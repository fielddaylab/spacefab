using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Music;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Handles post-load setup for the Supply minigame.
    /// Remains in SetupMask until the supply chain map scene has loaded,
    /// then transitions to SupplyMask.
    /// Runs on Update phase at order 0 under SetupMask.
    /// </summary>
    [PreloadOrder(10000)]
    public class SupplyLoader : MonoBehaviour, IScenePreload, ISceneLoadHandler {
        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.SupplyMask);
            ScriptUtility.Trigger(SupplyScriptTriggers.OnSupplySetupCompleted);
        }

        public IEnumerator<WorkSlicer.Result?> Preload() {
            Find.State(out SupplyChainMap map, out ChapterState chapterState, out SupplyMinigameState supplyState);

            int chapterIndex = chapterState.ChapterIndex;

            var entry = map.Entries[chapterIndex];

            Find.State(out SupplyShipIndex shipIndex);
            shipIndex.ShipCount = entry.ShipIds.Length;
            for (int i = 0; i < shipIndex.ShipCount; i++) {
                SupplyShipAsset shipAsset = Find.NamedAsset<SupplyShipAsset>(entry.ShipIds[i]);
                shipIndex.ShipAssets[i] = shipAsset;
                shipIndex.ShipStats[i] = new SupplyShipStats() {
                    Capacity = shipAsset.Capacity,
                    Speed = shipAsset.Speed
                };
            }

            yield return null;

            SupplyChainUtility.PopulateShipList(Find.Panel<ShipListPanel>(), shipIndex);

            yield return null;

            foreach (var data in entry.Positions) {
                SupplyRouteNode node = SupplyRouteUtility.GetNodeForId(data.Name);
                node.transform.localPosition = data.Position;
                node.gameObject.SetActive(true);
            }

            // TODO: apply overrides

            foreach (var node in map.Nodes) {
                node.Position = node.transform.localPosition;
                if (node.Type == SupplyRouteNodeType.Home && node.gameObject.activeSelf) {
                    Assert.True(!map.Home, "Cannot have multiple home nodes");
                    map.Home = node;
                }
            }

            Assert.True(map.Home, "No home node available!");

            supplyState.CurrSupplyChainMap = entry;
            yield return null;

            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.SupplyMask);
        }
    }
}

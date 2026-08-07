using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
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
        [Header("-- DEBUG --")]
        [Range(0, 12)] public int DebugChapterIndex;

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.SupplyMask);
            ScriptUtility.Trigger(SupplyScriptTriggers.OnSupplySetupCompleted);
        }

        public IEnumerator<WorkSlicer.Result?> Preload() {
            Find.State(out SupplyChainMap map, out ChapterState chapterState, out SupplyMinigameState supplyState);

            int chapterIndex = chapterState.ChapterIndex;
            if (DebugFlags.LaunchedFromThisScene) {
                chapterIndex = DebugChapterIndex;
            }

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

            foreach (var data in entry.Hazards) {
                SupplyRouteHazard hazard = SupplyRouteUtility.GetHazardForId(data.Name);
                hazard.transform.localPosition = data.Position;
                hazard.gameObject.SetActive(true);
            }

            foreach (var data in entry.Overrides) {
                SupplyRouteNode node = SupplyRouteUtility.GetNodeForId(data.Name);
                node.Time = data.Time;
                node.Cost = data.Cost;
                node.Risk = data.Risk;
            }

            foreach (var node in map.Nodes) {
                node.Position = node.transform.localPosition;
                if (node.Type == SupplyRouteNodeType.Home && node.gameObject.activeSelf) {
                    Assert.True(!map.Home, "Cannot have multiple home nodes");
                    map.Home = node;
                }
            }

            map.NodeCount = entry.Positions.Length;
            map.HazardCount = entry.Hazards.Length;

            Assert.True(map.Home, "No home node available!");

            supplyState.CurrSupplyChainMap = entry;
            yield return null;

            // set up camera bounding region
            Find.State(out SupplyCameraControlState cameraState);
            cameraState.Region = entry.CameraBounds;

            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.SupplyMask);
        }
    }
}

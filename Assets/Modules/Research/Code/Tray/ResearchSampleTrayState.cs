using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Holds the Research minigame's sample tray: the prefab to spawn for each
    /// material, the parent transform spawned samples are placed under, the
    /// vertical spacing between them, and the runtime list of currently-spawned
    /// ResearchMaterialSource instances. Populated by ResearchSampleTrayUtility
    /// during minigame setup and torn down on exit / re-entry.
    /// </summary>
    public class ResearchSampleTrayState : SharedStateComponent, IRegistrationCallbacks {
        public Transform Root;
        public float Spacing = 1f;
        public GameObject SamplePrefab;

        // Box collider defining the tray's drop region. A dragged instance
        // dropped anywhere inside this region returns to the pool, so the
        // player can return materials to the tray without aiming at a specific
        // source. The collider does not need to be on a specific Physics2D
        // layer; the drag system tests it directly via OverlapPoint.
        public Collider2D Region;

        [NonSerialized] public List<ResearchMaterialSource> SpawnedSamples;

        public void OnRegister() {
            if (SpawnedSamples == null) {
                SpawnedSamples = new List<ResearchMaterialSource>();
            }
        }

        public void OnDeregister() {

        }
    }

    /// <summary>
    /// Logic paired with ResearchSampleTrayState. SpawnTray rebuilds the tray
    /// from the chapter's available-materials list; ClearTray destroys the
    /// current set of gems. SpawnTray is idempotent (clears first), so callers
    /// don't have to coordinate teardown on re-entry.
    /// </summary>
    public static class ResearchSampleTrayUtility {
        // Rebuilds the tray from researchState.AvailableMaterials. Destroys any
        // existing samples, then spawns one prefab per material id, configures
        // its source + rig, and lays them out vertically along Y under
        // trayState.Root. Missing assets are warned-and-skipped, not fatal.
        public static void SpawnTray(ResearchSampleTrayState trayState, ResearchMinigameState researchState) {
            if (trayState == null || researchState == null) {
                return;
            }
            if (trayState.SamplePrefab == null) {
                Debug.LogWarning("[ResearchSampleTrayUtility] SamplePrefab is not assigned; skipping tray spawn.");
                return;
            }
            if (trayState.Root == null) {
                Debug.LogWarning("[ResearchSampleTrayUtility] Root is not assigned; skipping tray spawn.");
                return;
            }

            // 2. Spawn one prefab per available material id.
            int index = 0;
            foreach (StringHash32 id in researchState.AvailableMaterials) {
                MaterialAsset material = Find.NamedAsset<MaterialAsset>(id);
                if (material == null) {
                    Debug.LogWarningFormat("[ResearchSampleTrayUtility] No MaterialAsset registered for id '{0}'; skipping tray gem.", id.ToDebugString());
                    continue;
                }

                GameObject sampleObj = UnityEngine.Object.Instantiate(trayState.SamplePrefab, trayState.Root);

                // 2a. Wire the tray source. Sources are permanent fixtures;
                // the player clicks them to allocate a draggable instance.
                ResearchMaterialSource source = sampleObj.GetComponent<ResearchMaterialSource>();
                if (source != null) {
                    source.Material = material;
                    trayState.SpawnedSamples.Add(source);
                } else {
                    Debug.LogWarningFormat(sampleObj, "[ResearchSampleTrayUtility] Spawned prefab is missing ResearchMaterialSource; tray gem will not be clickable.");
                }

                // 2b. Apply per-material visuals via the rig utility. Missing
                // rig is non-fatal — a source without visuals still works for
                // click-to-lift, just invisible.
                ResearchMaterialVisualRig rig = sampleObj.GetComponent<ResearchMaterialVisualRig>();
                if (rig != null) {
                    ResearchMaterialVisualRigUtility.ApplyPropertiesToRig(rig, material, researchState);
                }

                // 2c. Vertical layout, top-down: index 0 sits at Root, each
                // subsequent gem moves down by Spacing on Y.
                sampleObj.transform.localPosition = new Vector3(0f, -index * trayState.Spacing, 0f);
                index++;
            }
        }
    }
}

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

        [NonSerialized] public List<ResearchMaterialSource> SpawnedSamples;

        public void OnRegister() {
            if (SpawnedSamples == null) {
                SpawnedSamples = new List<ResearchMaterialSource>();
            }
        }

        public void OnDeregister() {
            // Tear down spawned gems before the state itself disappears so we
            // don't leak GameObjects across scene transitions.
            ResearchSampleTrayUtility.ClearTray(this);
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

            // 1. Clear existing samples so re-entry rebuilds without leaks.
            ClearTray(trayState);

            // 2. Spawn one prefab per available material id.
            int index = 0;
            foreach (StringHash32 id in researchState.AvailableMaterials) {
                MaterialAsset material = Find.NamedAsset<MaterialAsset>(id);
                if (material == null) {
                    Debug.LogWarningFormat("[ResearchSampleTrayUtility] No MaterialAsset registered for id '{0}'; skipping tray gem.", id.ToDebugString());
                    continue;
                }

                GameObject go = UnityEngine.Object.Instantiate(trayState.SamplePrefab, trayState.Root);

                // 2a. Wire the draggable source. CurrentSlot stays null because
                // tray gems are free-floating until the player picks them up.
                ResearchMaterialSource source = go.GetComponent<ResearchMaterialSource>();
                if (source != null) {
                    source.Material = material;
                    source.CurrentSlot = null;
                    trayState.SpawnedSamples.Add(source);
                } else {
                    Debug.LogWarningFormat(go, "[ResearchSampleTrayUtility] Spawned prefab is missing ResearchMaterialSource; tray gem will not be draggable.");
                }

                // 2b. Apply per-material visuals via the existing rig utility.
                // Missing rig is non-fatal — a draggable without visuals still
                // works for the drag loop.
                ResearchMaterialRig rig = go.GetComponent<ResearchMaterialRig>();
                if (rig != null) {
                    ResearchMaterialRigUtility.ApplyPropertiesToRig(rig, material);
                }

                // 2c. Vertical layout, top-down: index 0 sits at Root, each
                // subsequent gem moves down by Spacing on Y.
                go.transform.localPosition = new Vector3(0f, -index * trayState.Spacing, 0f);
                index++;
            }
        }

        // Destroys every spawned sample tracked by trayState and empties the
        // SpawnedSamples list. Safe to call on a never-spawned tray.
        public static void ClearTray(ResearchSampleTrayState trayState) {
            if (trayState == null || trayState.SpawnedSamples == null) {
                return;
            }
            for (int i = 0; i < trayState.SpawnedSamples.Count; i++) {
                ResearchMaterialSource source = trayState.SpawnedSamples[i];
                if (source != null) {
                    UnityEngine.Object.Destroy(source.gameObject);
                }
            }
            trayState.SpawnedSamples.Clear();
        }
    }
}

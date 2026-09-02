using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
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
        public float XSpacing = 1.1f;
        public float YSpacing = 1.2f;
        public GameObject SamplePrefab;
        public MaterialAtom SampleAtomicView;
        public MaterialPolyelementalAtom PolyelementalSampleAtomicView;

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

                    // Stamp the onboarding ElementTag id so Leaf tutorial calls can address
                    // this sample by its long material name ("research:sample-copper", ...).
                    // DisplayName is lowercased to match the "module:kebab-case-name" id format
                    // used elsewhere in the project.
                    if (source.Tag != null) {
                        StringHash32 tagId = string.IsNullOrEmpty(material.name)
                            ? default
                            : new StringHash32("research:sample-" + material.name.ToLowerInvariant());
                        source.Tag.SetId(tagId);
                    }

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

                // 3a. Spawn doping chamber view
                bool known = researchState != null
                    && researchState.SandboxProperties.TryGetValue(material.AssetId, out var dopantRecord)
                    && !MaterialPropertyRecordUtility.IsEmpty(dopantRecord);

                ResearchMaterialView materialView = Find.NamedAsset<ResearchMaterialView>(material.AssetId);
                Assert.False(materialView == null, $"[ResearchSampleTrayUtility] Missing research material view for {material.AssetId.ToDebugString()}");
                
                if (material.ConstituentElementNames.Length == 0) {
                    MaterialAtom atom = UnityEngine.Object.Instantiate(trayState.SampleAtomicView, source.AtomicView);
                    atom.MaterialSprite.color = materialView.AtomColor[0];
                    atom.Label.text = known ? material.ShortName : "?";
                    for (int i = 0; i < atom.ElectronSprites.Length; i++) {
                        SpriteRenderer electron = atom.ElectronSprites[i];
                        electron.SetAlpha (i < material.ValenceElectronCounts[0] ? 1f : 0f);
                    }
                }
                else {
                    MaterialPolyelementalAtom atom = UnityEngine.Object.Instantiate(trayState.PolyelementalSampleAtomicView, source.AtomicView);
                    for (int i = 0; i < atom.MaterialAtoms.Length; i++) {
                        atom.MaterialAtoms[i].MaterialSprite.color = materialView.AtomColor[i];
                        atom.Label.text = known ? material.ShortName : "?";
                        for (int e = 0; e < atom.MaterialAtoms[i].ElectronSprites.Length; e++) {
                            SpriteRenderer electron = atom.MaterialAtoms[i].ElectronSprites[e];
                            electron.SetAlpha(e < material.ValenceElectronCounts[i] ? 1f : 0f);
                        }
                    }
                }

                // TODO: debugging purpose
                source.Rig.gameObject.SetActive(false);
                source.AtomicView.gameObject.SetActive(true);

                // 2c. Vertical layout, top-down: index 0 sits at Root, each
                // subsequent gem moves down by Spacing on Y.
                float startX = -0.2f;
                float startY = 3.2f;
                sampleObj.transform.localPosition = new Vector3(startX + index % 2 * trayState.XSpacing, startY - index / 2 * trayState.YSpacing, 0f);
                index++;
            }
        }
    }
}

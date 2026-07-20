using BeauUtil;
using FieldDay;
using FieldDay.Components;
using SpaceFab.Materials;
using TMPro;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Visual rig for a Research-minigame gem. Holds the renderer + transform
    /// hierarchy used by both slot-bound items and the cursor-following drag
    /// preview. Pure data — render application logic lives in
    /// ResearchMaterialVisualRigUtility.
    /// </summary>
    public class ResearchMaterialVisualRig : BatchedComponent {
        [Header("Body")]
        public SpriteRenderer Renderer;
        public Transform RendererPosition;

        [Header("Shadow")]
        public SpriteRenderer ShadowRenderer;
        public Transform ShadowPosition;

        [Header("Other")]
        public TMP_Text Label;
        public GameObject Highlight;
    }

    /// <summary>
    /// Logic paired with ResearchMaterialVisualRig. Translates a MaterialAsset and
    /// its ResearchMaterialView into renderer sprite, color, scale, rotation,
    /// and label. Falls back gracefully if no view is registered.
    /// </summary>
    public static class ResearchMaterialVisualRigUtility {
        // Applies a material's visual properties to the rig. View is
        // looked up by MaterialAsset.AssetId; a missing view leaves the
        // rig untouched and logs a warning. researchState is consulted
        // for the "is this material known?" check (any sandbox property
        // confirmed); pass null when not available — the rig falls back
        // to the unknown label (sample number).
        public static void ApplyPropertiesToRig(ResearchMaterialVisualRig rig, MaterialAsset material, ResearchMinigameState researchState) {
            if (rig == null) {
                return;
            }
            if (material == null) {
                ClearRig(rig);
                return;
            }

            if (material.GemSprite == null) {
                Debug.LogWarningFormat(rig, "[ResearchMaterialVisualRig] No gem sprite registered for material '{0}'; rig will keep its current sprite.", material.name);
                return;
            }

            // 1. Sprite selection — single vs. multi-atom path.
            Sprite bodySprite = material.GemSprite;
            if (rig.Renderer != null) {
                rig.Renderer.sprite = bodySprite;
                // rig.Renderer.color = view.GemColor;
            }
            if (rig.ShadowRenderer != null) {
                rig.ShadowRenderer.sprite = bodySprite;
            }

            // 3. Uniform scale, baked into the view.
            // Vector3 scale = new Vector3(view.GemScale, view.GemScale, 1f);
            // if (rig.RendererPosition != null) {
            //     rig.RendererPosition.localScale = scale;
            // }
            // if (rig.ShadowPosition != null) {
            //     rig.ShadowPosition.localScale = scale;
            // }

            // 4. Label: ShortName once any property is confirmed for
            // this material in the sandbox; sample number until then.
            if (rig.Label != null) {
                bool known = researchState != null
                    && researchState.SandboxProperties.TryGetValue(material.AssetId, out var record)
                    && !MaterialPropertyRecordUtility.IsEmpty(record);
                ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(material.AssetId);
                //rig.Label.SetText(known ? material.ShortName : view.SampleNumber.ToString());
                rig.Label.SetText(known ? material.ShortName : view.SampleLabel.ToString());
            }
        }

        // Clears the rig's visual content. Used when the held material becomes
        // null (drag cancelled, slot cleared, etc.).
        public static void ClearRig(ResearchMaterialVisualRig rig) {
            if (rig.Renderer != null) {
                rig.Renderer.sprite = null;
            }
            if (rig.ShadowRenderer != null) {
                rig.ShadowRenderer.sprite = null;
            }
            if (rig.Label != null) {
                rig.Label.SetText(string.Empty);
            }
        }
    }
}

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
    /// ResearchMaterialRigUtility.
    /// </summary>
    public class ResearchMaterialRig : BatchedComponent {
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
    /// Logic paired with ResearchMaterialRig. Translates a MaterialAsset and
    /// its ResearchMaterialView into renderer sprite, color, scale, rotation,
    /// and label. Falls back gracefully if no view is registered.
    /// </summary>
    public static class ResearchMaterialRigUtility {
        // Applies a material's visual properties to the rig. View is looked up
        // by MaterialAsset.AssetId; a missing view leaves the rig untouched
        // and logs a warning.
        public static void ApplyPropertiesToRig(ResearchMaterialRig rig, MaterialAsset material) {
            if (rig == null) {
                return;
            }
            if (material == null) {
                ClearRig(rig);
                return;
            }

            ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(material.AssetId);
            if (view == null) {
                Debug.LogWarningFormat(rig, "[ResearchMaterialRig] No ResearchMaterialView registered for material '{0}'; rig will keep its current sprite.", material.name);
                return;
            }

            // 1. Sprite selection — single vs. multi-atom path.
            Sprite bodySprite = view.IsMultiAtom ? view.MultiAtomSprite : view.SingleAtomSprite;
            if (rig.Renderer != null) {
                rig.Renderer.sprite = bodySprite;
                rig.Renderer.color = view.GemColor;
            }
            if (rig.ShadowRenderer != null) {
                rig.ShadowRenderer.sprite = bodySprite;
            }

            // 2. Hash-derived rotation: deterministic per-material variation
            // so two gems of the same material always face the same way.
            float rotation = (material.AssetId.HashValue) / (float)uint.MaxValue;
            Quaternion rot = Quaternion.Euler(0, 0, rotation * 360f);
            if (rig.RendererPosition != null) {
                rig.RendererPosition.localRotation = rot;
            }
            if (rig.ShadowPosition != null) {
                rig.ShadowPosition.localRotation = rot;
            }

            // 3. Uniform scale, baked into the view.
            Vector3 scale = new Vector3(view.GemScale, view.GemScale, 1f);
            if (rig.RendererPosition != null) {
                rig.RendererPosition.localScale = scale;
            }
            if (rig.ShadowPosition != null) {
                rig.ShadowPosition.localScale = scale;
            }

            // 4. Label uses the material's display name.
            if (rig.Label != null) {
                rig.Label.SetText(material.DisplayName);
            }
        }

        // Clears the rig's visual content. Used when the held material becomes
        // null (drag cancelled, slot cleared, etc.).
        public static void ClearRig(ResearchMaterialRig rig) {
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

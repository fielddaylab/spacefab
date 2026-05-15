using FieldDay;
using FieldDay.HID;
using FieldDay.UI;
using SpaceFab.Materials;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Research {
    /// <summary>
    /// One chip in the hypothesis or observations panel. Pure view: a
    /// background Image, a TMP label, an optional CursorHint for chips
    /// that respond to clicks (sample-panel slots and chip-picker
    /// entries), and an optional LockedOverlay shown when an auto-
    /// populated slot cannot be removed.
    ///
    /// All visual data — sprites per ObservationType + the two label
    /// colors — lives on a global ResearchObservationChipAsset that this
    /// chip reads at every SetState call. Nothing is baked into the
    /// prefab.
    /// </summary>
    public class ResearchObservationChip : MonoBehaviour {
        public Image Background;
        public TMP_Text LabelText;

        public GameObject LockedOverlay;
        public CursorHint Click;

        // Applies the chip's appearance from the global visual asset.
        // observationType selects the sprite pair; filled selects within
        // that pair and the label color; text == null hides the label.
        public void SetState(string text, bool filled, bool locked, ObservationType observationType) {
            ResearchObservationChipAssets asset = Find.GlobalAsset<ResearchObservationChipAssets>();

            if (LabelText != null) {
                LabelText.text = string.IsNullOrEmpty(text) ? string.Empty : text;
                if (asset != null) {
                    LabelText.color = filled ? asset.LabelFilledColor : asset.LabelEmptyColor;
                }
            }

            if (Background != null) {
                if (asset != null && asset.TryGetSprite(observationType, filled, out Sprite sprite)) {
                    Background.sprite = sprite;
                    Background.enabled = true;
                } else {
                    // No sprite registered for this observation type yet —
                    // hide the chip background so it doesn't render as a
                    // default white square.
                    Background.enabled = false;
                }
            }

            if (LockedOverlay != null) {
                LockedOverlay.SetActive(filled && locked);
            }
        }
    }
}

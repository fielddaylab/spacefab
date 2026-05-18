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
        // useEmptyDashedSprite swaps the per-type empty sprite for the
        // shared dashed-outline EmptySlotSprite when filled == false —
        // used by sample-panel slot chips to indicate "the hypothesis
        // requires a value here, but the player hasn't picked one yet."
        public void SetState(string text, bool filled, bool locked, ObservationType observationType, bool useEmptyDashedSprite = false) {
            ResearchObservationChipAssets asset = Find.GlobalAsset<ResearchObservationChipAssets>();

            if (LabelText != null) {
                LabelText.text = string.IsNullOrEmpty(text) ? string.Empty : text;
                if (asset != null) {
                    LabelText.color = filled ? asset.LabelFilledColor : asset.LabelEmptyColor;
                }
            }

            if (Background != null) {
                Sprite sprite = null;
                bool spriteOk = false;
                if (asset != null) {
                    if (!filled && useEmptyDashedSprite && asset.EmptySlotSprite != null) {
                        sprite = asset.EmptySlotSprite;
                        spriteOk = true;
                    } else {
                        spriteOk = asset.TryGetSprite(observationType, filled, out sprite);
                    }
                }
                if (spriteOk) {
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

        // Applies the "already filled in a sample slot" greyed-out look
        // for picker chips. Empty for now; the visual contract is TBD.
        // Called per-frame from ObservationPickerRefreshSystem when the
        // hypothesis viewmodel changes. When implemented, must also gate
        // Click so a disabled chip is non-interactive.
        public void SetPickerChipDisabledVisual(bool disabled) {
        }
    }
}

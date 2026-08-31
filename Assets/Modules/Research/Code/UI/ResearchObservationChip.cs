using FieldDay;
using FieldDay.HID;
using FieldDay.UI;
using SpaceFab.Materials;
using SpaceFab.Onboarding;
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

        // Onboarding highlight handle, left unassigned on the prefab. Chips are pool-allocated
        // and reused across pages, so neither the tag nor its id can be baked in: the wiki's
        // page-load utilities attach one on demand via WikiElementTagUtility, stamp a per-page
        // id, and clear it again on free. Allocators that don't tutorialize their chips (the
        // Research picker and sample panel) never touch this, so those chips stay untagged and
        // out of ElementTagLookup entirely. Assign it in the inspector to point the highlight at
        // a different host than the chip root.
        public ElementTag Tag;

        // Label color applied by the most recent SetState, so the
        // disabled visual can restore it when re-enabled.
        private Color m_BaseLabelColor;

        // Applies the chip's appearance from the global visual asset.
        // observationType selects the sprite set; fillState selects within
        // it and drives the label color; text == null hides the label.
        // useEmptyDashedSprite swaps the per-type empty sprite for the
        // shared dashed-outline EmptySlotSprite when the state is Empty —
        // used by sample-panel slot chips to indicate "the hypothesis
        // requires a value here, but the player hasn't picked one yet."
        public void SetState(string text, ChipFillState fillState, bool locked, ObservationType observationType, bool useEmptyDashedSprite = false) {
            ResearchObservationChipAssets asset = Find.GlobalAsset<ResearchObservationChipAssets>();
            bool empty = fillState == ChipFillState.Empty;

            if (LabelText != null) {
                LabelText.text = string.IsNullOrEmpty(text) ? string.Empty : text;
                if (asset != null) {
                    LabelText.color = empty ? asset.LabelEmptyColor : asset.LabelFilledColor;
                }
                m_BaseLabelColor = LabelText.color;
            }

            if (Background != null) {
                Background.color = Color.white;

                Sprite sprite = null;
                bool spriteOk = false;
                if (asset != null) {
                    if (empty && useEmptyDashedSprite && asset.EmptySlotSprite != null) {
                        sprite = asset.EmptySlotSprite;
                        spriteOk = true;
                    } else {
                        spriteOk = asset.TryGetSprite(observationType, fillState, out sprite);
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
                LockedOverlay.SetActive(!empty && locked);
            }
        }

        // Applies the "already selected in the sample panel" greyed-out
        // look. Click gating is deliberately NOT done here — a greyed
        // wiki chip stays clickable, and clicking it removes the
        // selection; any inertness is the registering caller's
        // responsibility. Restoring uses the label color cached by the
        // last SetState, so callers that override the label color after
        // SetState (wiki characteristics chips) must not toggle this.
        public void SetPickerChipDisabledVisual(bool disabled) {
            ResearchObservationChipAssets asset = Find.GlobalAsset<ResearchObservationChipAssets>();
            if (asset == null) {
                return;
            }

            if (Background != null) {
                Background.color = disabled ? asset.ChipDisabledTint : Color.white;
            }
            if (LabelText != null) {
                LabelText.color = disabled ? asset.LabelDisabledColor : m_BaseLabelColor;
            }
        }
    }
}

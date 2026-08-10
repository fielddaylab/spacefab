using FieldDay.Assets;
using SpaceFab.Materials;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Which of an ObservationSpritePair's three sprites a chip wears.
    /// Empty is a placeholder the player hasn't filled in; Filled is a
    /// known value; Confirmed is a known value the game has verified, and
    /// carries the green checkmark.
    /// </summary>
    public enum ChipFillState {
        Empty,
        Filled,
        Confirmed,
    }

    /// <summary>
    /// Global lookup the observation chip widget reads at render time.
    /// Holds a per-ObservationType (empty, filled) sprite pair plus the
    /// two label-text colors that are shared across every observation
    /// type. Authoring stays in the asset; the chip prefab itself
    /// carries only inspector refs to its Image / TMP_Text / overlay
    /// GameObject. Adding a new observation sprite set means editing
    /// this asset, not every chip prefab in the scene.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/Observation Chip Visuals")]
    public class ResearchObservationChipAssets : GlobalAsset {
        [Serializable]
        public struct ObservationSpritePair {
            public ObservationType ObservationType;
            public Sprite EmptyChip; // empty slot -- only shape, no color
            public Sprite FilledChip; // shape and color
            public Sprite ConfirmedChip; // shape and green + checkmark
        }

        [SerializeField] private ObservationSpritePair[] m_ObservationSprites;
        [SerializeField] private Color m_LabelFilledColor = Color.black;
        [SerializeField] private Color m_LabelEmptyColor = new Color(0f, 0f, 0f, 0.5f);

        // Greyed-out styling for chips whose observation/property is
        // already selected in the sample panel. The tint multiplies the
        // background sprite; the label color replaces the filled/empty
        // color outright.
        [SerializeField] private Color m_ChipDisabledTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color m_LabelDisabledColor = new Color(0f, 0f, 0f, 0.4f);

        // Generic dashed-outline sprite used for sample-panel slots
        // that exist (the active hypothesis page has a leaf for them)
        // but haven't been satisfied yet. Shared across all
        // ObservationTypes — slot-empty visuals don't carry type info.
        [SerializeField] private Sprite m_EmptySlotSprite;

        private Dictionary<ObservationType, ObservationSpritePair> m_Lookup;

        public Color LabelFilledColor => m_LabelFilledColor;
        public Color LabelEmptyColor => m_LabelEmptyColor;
        public Color ChipDisabledTint => m_ChipDisabledTint;
        public Color LabelDisabledColor => m_LabelDisabledColor;

        public Sprite EmptySlotSprite => m_EmptySlotSprite;

        public override void Mount() {
            int count = m_ObservationSprites != null ? m_ObservationSprites.Length : 0;
            m_Lookup = new Dictionary<ObservationType, ObservationSpritePair>(count);
            for (int i = 0; i < count; i++) {
                m_Lookup[m_ObservationSprites[i].ObservationType] = m_ObservationSprites[i];
            }
        }

        public override void Unmount() {
            m_Lookup = null;
        }

        /// <summary>
        /// Resolves the chip sprite for (observationType, fillState).
        /// Returns false when no entry is registered for the type, or
        /// when the registered entry has a null sprite — chip callers
        /// hide the Image in that case.
        /// </summary>
        public bool TryGetSprite(ObservationType observationType, ChipFillState fillState, out Sprite sprite) {
            if (m_Lookup != null && m_Lookup.TryGetValue(observationType, out var pair)) {
                switch (fillState) {
                    case ChipFillState.Confirmed:
                        sprite = pair.ConfirmedChip;
                        break;
                    case ChipFillState.Filled:
                        sprite = pair.FilledChip;
                        break;
                    default:
                        sprite = pair.EmptyChip;
                        break;
                }
                return sprite != null;
            }
            sprite = null;
            return false;
        }
    }
}

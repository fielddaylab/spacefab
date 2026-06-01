using BeauPools;
using BeauUtil;
using FieldDay;
using SpaceFab.Materials;
using SpaceFab.Research;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Rebuilds the material-characteristics chip column on a wiki
    /// material page. Mirrors ObservationPickerLoadUtility's shape:
    /// free prior chips, alloc one per confirmed property, lay them
    /// out vertically, resize the group to fit.
    ///
    /// Called by WikiCharacteristicsRefreshSystem when the active
    /// wiki page changes to a material page, or when a new property
    /// is confirmed (Research-side) and the wiki is open on a
    /// material page.
    /// </summary>
    public static class WikiCharacteristicsLoadUtility {
        // Padding (px) added above and below the chip column when
        // resizing MaterialCharacteristicsGroup's RectTransform.
        private const float OverlayPadding = 16f;

        // Gap (px) between adjacent chips. Mirrors the Research
        // picker chip gap so the two columns read as a single visual
        // language.
        private const float ChipGap = 8f;

        // Label shown on an unconfirmed-but-discoverable chip slot.
        private const string UnknownLabelText = "?";

        // Frees any previously-allocated characteristic chips, then
        // allocs one chip per discoverable static persistent property
        // on the material (sourced from MaterialAsset.Properties). For
        // each, the chip renders as filled with the property's name
        // when confirmed in the merged record (PlayerProgress +
        // Research sandbox if loaded), or as empty with a "?" label
        // when the player hasn't discovered it yet.
        //
        // Dynamic labels (PDopantFor / NDopantFor) are skipped from
        // the placeholder pass because MaterialAsset.Properties has
        // no associated context. Confirmed dynamic entries from the
        // merged record are still appended afterward so they show up
        // once the player has discovered them.
        //
        // materialId == Null or no MaterialAsset registered => zero
        // chips + minimum group size (just padding).
        public static void LoadFor(WikiPageContentWidgets widgets, WikiChipPools pools, StringHash32 materialId) {
            if (widgets == null || pools == null || pools.ChipPool == null) return;
            if (widgets.CharacteristicsContainer == null) return;

            // 1. Free prior chips.
            FreeAllCharacteristicChips(pools);

            // 2. Merge knowledge sources. Start from canonical
            //    PlayerProgressState; OR-merge ResearchMinigameState
            //    sandbox if Research is currently loaded.
            MaterialPropertyRecord merged = default;
            PlayerProgressState progress = Find.State<PlayerProgressState>();
            if (progress != null && progress.MaterialProperties != null
                && progress.MaterialProperties.TryGetValue(materialId, out var canonicalRecord)) {
                merged = canonicalRecord;
            }
            if (Game.SharedState.Has<ResearchMinigameState>()) {
                ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
                if (researchState.SandboxProperties != null
                    && researchState.SandboxProperties.TryGetValue(materialId, out var sandboxRecord))
                {
                    MaterialPropertyRecordUtility.Merge(ref merged, sandboxRecord);
                }
            }

            // 3. Walk the material's ground-truth Properties[] for
            // the static-persistent placeholder pass.
            MaterialAsset material = Find.NamedAsset<MaterialAsset>(materialId);
            if (material != null && material.Properties != null) {
                for (int i = 0; i < material.Properties.Length; i++) {
                    MaterialPropertyLabel label = material.Properties[i];
                    if (!MaterialPropertyLabelUtility.IsPersistent(label)) continue;
                    if (MaterialPropertyLabelUtility.IsDynamic(label)) continue;

                    bool confirmed = MaterialPropertyRecordUtility.Has(merged, label, StringHash32.Null);
                    AddChip(widgets, pools,
                        text: confirmed ? MaterialPropertyLabelDisplay.GetPropertyName(label) : UnknownLabelText,
                        filled: confirmed);
                }
            }

            // 4. Confirmed dynamic-label entries from the merged
            // record. No placeholder pass for these — only show when
            // known. (PDopantFor / NDopantFor — both filled.)
            AppendConfirmedDynamic(widgets, pools, merged, MaterialPropertyLabel.PDopantFor);
            AppendConfirmedDynamic(widgets, pools, merged, MaterialPropertyLabel.NDopantFor);

            // 5. Lay out + resize the group.
            float contentHeight = ResearchUILayoutUtility.LayoutVerticalCentered(
                pools.ActiveCharacteristicChips, pools.ActiveCharacteristicChips.Count, ChipGap);
            if (widgets.MaterialCharacteristicsGroup != null) {
                RectTransform groupRect = widgets.MaterialCharacteristicsGroup.transform as RectTransform;
                if (groupRect != null) {
                    Vector2 size = groupRect.sizeDelta;
                    size.y = contentHeight + 2f * OverlayPadding;
                    groupRect.sizeDelta = size;
                }
            }
        }

        // Allocs one chip with the given text + filled state, parents
        // it under the container, registers in the active list.
        // ObservationType.ConfirmedProperty selects the dedicated
        // sprite bucket on ResearchObservationChipAssets — same
        // bucket is used for filled and empty so the chip's frame
        // matches across the discoverable list. The label color is
        // forced black after SetState — the global LabelEmptyColor
        // is white (intended for picker / slot chips on darker
        // backgrounds), but the wiki characteristics group sits on a
        // light surface, so empty placeholders need black text to be
        // legible.
        private static void AddChip(WikiPageContentWidgets widgets, WikiChipPools pools, string text, bool filled) {
            ResearchObservationChip chip = pools.ChipPool.Alloc();
            if (chip == null) return;
            chip.transform.SetParent(widgets.CharacteristicsContainer, false);
            chip.SetState(text, filled, false, ObservationType.ConfirmedProperty);
            if (chip.LabelText != null) {
                chip.LabelText.color = Color.black;
            }
            pools.ActiveCharacteristicChips.Add(chip);
        }

        // Emits a filled chip for every bit set in the record's
        // dynamic mask for the given dynamic label. Bit index →
        // material id via MaterialOrderAsset. Skipped silently if
        // the registry is missing.
        private static void AppendConfirmedDynamic(WikiPageContentWidgets widgets, WikiChipPools pools, in MaterialPropertyRecord record, MaterialPropertyLabel dynamicLabel) {
            ushort mask = dynamicLabel == MaterialPropertyLabel.PDopantFor ? record.DynamicMask_PDopant : record.DynamicMask_NDopant;
            if (mask == 0) return;

            MaterialOrderAsset materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            if (materialOrder == null) return;
            int orderCount = materialOrder.Count;

            for (int bit = 0; bit < 16 && mask != 0; bit++) {
                if ((mask & (1 << bit)) == 0) continue;
                mask &= unchecked((ushort)~(1 << bit));
                if (bit >= orderCount) continue;
                AddChip(widgets, pools, MaterialPropertyLabelDisplay.GetPropertyName(dynamicLabel), filled: true);
            }
        }

        // Returns every pool-held characteristic chip to the pool.
        // Called as the first step of LoadFor (clean slate); also
        // callable directly when navigating away from a material
        // page so chips don't stay parked under
        // CharacteristicsContainer.
        public static void FreeAllCharacteristicChips(WikiChipPools pools) {
            if (pools == null || pools.ActiveCharacteristicChips == null) return;
            int n = pools.ActiveCharacteristicChips.Count;
            for (int i = n - 1; i >= 0; i--) {
                ResearchObservationChip chip = pools.ActiveCharacteristicChips[i];
                if (chip != null) {
                    Pool.TryFree(chip);
                }
            }
            pools.ActiveCharacteristicChips.Clear();
        }
    }
}

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
    /// Called by WikiVisualsUtility.RefreshPageContent, immediately
    /// before it shows the characteristics group, so the column is
    /// never rendered empty. Reached whenever the PageContent domain
    /// is invalidated: a page change, an expand, a Research-side
    /// property confirmation, or a page lock / unlock.
    ///
    /// Conductivity and insulation are each authored twice — an
    /// introductory framing and a full one — and a material may carry
    /// both. Those pairs collapse to a single chip, picked from the
    /// wiki's current unlock set rather than from the material.
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
        // each, the chip renders confirmed (green, checkmarked) with
        // the property's name when it is set in the merged record
        // (PlayerProgress + Research sandbox if loaded), or empty with
        // a "?" label when the player hasn't discovered it yet.
        //
        // Dynamic labels (PDopantFor / NDopantFor) are skipped from
        // the placeholder pass because MaterialAsset.Properties has
        // no associated context. Confirmed dynamic entries from the
        // merged record are still appended afterward so they show up
        // once the player has discovered them.
        //
        // A material authoring both halves of an exclusive pair
        // (ConductorNaive + Conductor, InsulatorNaive + Insulator)
        // contributes one chip for the pair, not two — see
        // IsRetiredPairHalf.
        //
        // materialId == Null or no MaterialAsset registered => zero
        // chips + minimum group size (just padding).
        public static void LoadFor(WikiPageContentWidgets widgets, WikiChipPools pools, PlayerProgressState progressState, StringHash32 materialId) {
            if (widgets == null || pools == null || pools.ChipPool == null) return;
            if (widgets.CharacteristicsContainer == null) return;

            // 1. Free prior chips.
            FreeAllCharacteristicChips(pools);

            // 2. Merge knowledge sources. Start from canonical
            //    PlayerProgressState; OR-merge ResearchMinigameState
            //    sandbox if Research is currently loaded.
            MaterialPropertyRecord merged = default;
            if (progressState != null && progressState.MaterialProperties != null
                && progressState.MaterialProperties.TryGetValue(materialId, out var canonicalRecord)) {
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
                    if (IsRetiredPairHalf(material.Properties, label, progressState)) continue;

                    bool confirmed = MaterialPropertyRecordUtility.Has(merged, label, StringHash32.Null);
                    AddChip(widgets, pools,
                        text: confirmed ? MaterialPropertyLabelDisplay.GetPropertyName(label) : UnknownLabelText,
                        fillState: confirmed ? ChipFillState.Confirmed : ChipFillState.Filled,
                        // Tagged off the label rather than the rendered text, so an undiscovered
                        // "?" slot is addressable under the same id it will carry once confirmed.
                        tagId: WikiElementTagUtility.MaterialCharacteristicId(materialId, label));
                }
            }

            // 4. Confirmed dynamic-label entries from the merged
            // record. No placeholder pass for these — only show when
            // known. (PDopantFor / NDopantFor — both filled.)
            AppendConfirmedDynamic(widgets, pools, merged, materialId, MaterialPropertyLabel.PDopantFor);
            AppendConfirmedDynamic(widgets, pools, merged, materialId, MaterialPropertyLabel.NDopantFor);

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

        // True when `label` is the half of an exclusive pair the wiki
        // isn't presenting right now, so the placeholder pass should
        // skip it and let the other half stand for the characteristic.
        //
        // Only applies to a material authoring both halves. One half on
        // its own always chips: the pair rule picks between two chips
        // that would otherwise render the same name twice, it doesn't
        // gate a characteristic on a page being unlocked.
        private static bool IsRetiredPairHalf(MaterialPropertyLabel[] properties, MaterialPropertyLabel label, PlayerProgressState progressState) {
            if (!TryGetExclusivePair(label, out MaterialPropertyLabel basic, out MaterialPropertyLabel full, out StringHash32 basicPageId)) {
                return false;
            }
            if (!ContainsLabel(properties, basic) || !ContainsLabel(properties, full)) { return false; }

            // The basic half holds the slot for as long as its page is
            // unlocked. That covers the opening state, where both pages
            // are unlocked and chapter 1 hasn't locked the full one yet —
            // the chapter the basic pages belong to. Once content retires
            // the basic page, the full half takes over, and it also
            // stands in should neither page be unlocked, so the
            // characteristic never drops off the column entirely.
            MaterialPropertyLabel visible = WikiUtility.IsPageUnlocked(progressState, basicPageId) ? basic : full;
            return label != visible;
        }

        // The exclusive pair `label` belongs to, plus the wiki page id
        // that decides which half of it renders. False for every label
        // outside the two pairs, which is all but four of them.
        private static bool TryGetExclusivePair(MaterialPropertyLabel label, out MaterialPropertyLabel basic, out MaterialPropertyLabel full, out StringHash32 basicPageId) {
            switch (label) {
                case MaterialPropertyLabel.ConductorNaive:
                case MaterialPropertyLabel.Conductor:
                    basic = MaterialPropertyLabel.ConductorNaive;
                    full = MaterialPropertyLabel.Conductor;
                    basicPageId = WikiConsts.BasicConductorPageId;
                    return true;

                case MaterialPropertyLabel.InsulatorNaive:
                case MaterialPropertyLabel.Insulator:
                    basic = MaterialPropertyLabel.InsulatorNaive;
                    full = MaterialPropertyLabel.Insulator;
                    basicPageId = WikiConsts.BasicInsulatorPageId;
                    return true;

                default:
                    basic = default;
                    full = default;
                    basicPageId = default;
                    return false;
            }
        }

        // Whether the material authors the given property at all.
        private static bool ContainsLabel(MaterialPropertyLabel[] properties, MaterialPropertyLabel label) {
            for (int i = 0; i < properties.Length; i++) {
                if (properties[i] == label) { return true; }
            }
            return false;
        }

        // Allocs one chip with the given text + fill state, parents
        // it under the container, registers in the active list.
        // ObservationType.ConfirmedProperty selects the dedicated
        // sprite bucket on ResearchObservationChipAssets — the same
        // bucket across every state so the chip's frame matches down
        // the discoverable list. The label color is forced black after
        // SetState — the global LabelEmptyColor is white (intended for
        // picker / slot chips on darker backgrounds), but the wiki
        // characteristics group sits on a light surface, so empty
        // placeholders need black text to be legible.
        //
        // tagId is the onboarding ElementTag id Leaf addresses this chip
        // by while the page is bound; null leaves the chip untagged.
        private static void AddChip(WikiPageContentWidgets widgets, WikiChipPools pools, string text, ChipFillState fillState, string tagId) {
            ResearchObservationChip chip = pools.ChipPool.Alloc();
            if (chip == null) return;
            chip.transform.SetParent(widgets.CharacteristicsContainer, false);
            chip.SetState(text, fillState, false, ObservationType.ConfirmedProperty);
            if (chip.LabelText != null) {
                chip.LabelText.color = Color.black;
            }
            WikiElementTagUtility.Stamp(chip, tagId);
            pools.ActiveCharacteristicChips.Add(chip);
        }

        // Emits a filled chip for every bit set in the record's
        // dynamic mask for the given dynamic label. Bit index →
        // material id via MaterialOrderAsset. Skipped silently if
        // the registry is missing.
        //
        // Every chip here renders the same label text, so the tag id
        // carries the bit's context material to keep the ids distinct.
        private static void AppendConfirmedDynamic(WikiPageContentWidgets widgets, WikiChipPools pools, in MaterialPropertyRecord record, StringHash32 materialId, MaterialPropertyLabel dynamicLabel) {
            ushort mask = dynamicLabel == MaterialPropertyLabel.PDopantFor ? record.DynamicMask_PDopant : record.DynamicMask_NDopant;
            if (mask == 0) return;

            MaterialOrderAsset materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            if (materialOrder == null) return;
            int orderCount = materialOrder.Count;

            for (int bit = 0; bit < 16 && mask != 0; bit++) {
                if ((mask & (1 << bit)) == 0) continue;
                mask &= unchecked((ushort)~(1 << bit));
                if (bit >= orderCount) continue;
                AddChip(widgets, pools, MaterialPropertyLabelDisplay.GetPropertyName(dynamicLabel), ChipFillState.Confirmed,
                    WikiElementTagUtility.MaterialCharacteristicId(materialId, dynamicLabel, materialOrder.GetId(bit)));
            }
        }

        // Returns every pool-held characteristic chip to the pool,
        // clearing its onboarding tag id in lockstep so an off-screen
        // chip can't resolve a highlight. Called as the first step of
        // LoadFor (clean slate); also callable directly when navigating
        // away from a material page so chips don't stay parked under
        // CharacteristicsContainer.
        public static void FreeAllCharacteristicChips(WikiChipPools pools) {
            if (pools == null || pools.ActiveCharacteristicChips == null) return;
            int n = pools.ActiveCharacteristicChips.Count;
            for (int i = n - 1; i >= 0; i--) {
                ResearchObservationChip chip = pools.ActiveCharacteristicChips[i];
                if (chip != null) {
                    WikiElementTagUtility.Clear(chip);
                    Pool.TryFree(chip);
                }
            }
            pools.ActiveCharacteristicChips.Clear();
        }
    }
}

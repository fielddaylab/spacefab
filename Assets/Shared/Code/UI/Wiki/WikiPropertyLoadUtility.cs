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
    /// Rebuilds a wiki property page: the property itself as a chip, the
    /// page's body text, and the property's decomposed observations as a
    /// chip column.
    ///
    /// In the Research scene the property chip selects the hypothesis, and
    /// greys while it is the active one — clicking it then deselects. The
    /// decomposed-observation chips are never greyed (the page describes
    /// what the property requires, not what the player has collected);
    /// whether they are clickable at all is authored on
    /// ResearchUIAssets.PropertyPageObservationChipsClickable.
    ///
    /// The decomposition is read from the property-definition registry
    /// rather than any Research state, so the page renders identically in
    /// every scene. It follows the first registered definition — the same
    /// convention the hypothesis viewmodel uses for its leaf count.
    /// </summary>
    public static class WikiPropertyLoadUtility {
        // Padding (px) added above and below the chip column when resizing
        // PropertyGroup's RectTransform.
        private const float OverlayPadding = 16f;

        // Gap (px) between adjacent chips. Mirrors the other wiki columns.
        private const float ChipGap = 8f;

        // Scratch for the decomposition; page binds are rare and
        // single-threaded, so one shared buffer suffices.
        private static readonly List<MaterialObservationEntry> s_LeafScratch = new List<MaterialObservationEntry>(8);
        private static readonly StringHash32[] s_NullContext = new StringHash32[] { StringHash32.Null };

        public static void LoadFor(WikiPageContentWidgets widgets, WikiChipPools pools, WikiPageData page, in WikiResearchContext researchContext) {
            if (widgets == null || pools == null || pools.ChipPool == null) return;

            // 1. Free prior chips and unbind the authored property chip.
            FreeAllPropertyChips(widgets, pools);

            MaterialPropertyCheck check = page.PropertyCheck;
            if (check == null) return;

            // 2. Property chip. Greys while it is the active hypothesis;
            // clicking it then deselects. Context-agnostic on purpose — the
            // chip names a property, not a property-for-a-substrate.
            if (widgets.PropertyChip != null) {
                widgets.PropertyChip.SetState(
                    MaterialPropertyLabelDisplay.GetPropertyName(check.Label),
                    ChipFillState.Filled, false,
                    MaterialObservationChamberLookup.GetChamberType(check.Label));

                bool isActiveHypothesis = researchContext.Present
                    && researchContext.ViewModel.HypothesisSelected
                    && researchContext.ViewModel.HypothesisLabel == check.Label;
                widgets.PropertyChip.SetPickerChipDisabledVisual(isActiveHypothesis);

                // Addressable from Leaf as "wiki:property-conductor" for as long as this page is
                // bound. Re-stamped per page because the authored chip is reused across all of them.
                WikiElementTagUtility.Stamp(widgets.PropertyChip, WikiElementTagUtility.PropertyChipId(check.Label));

                if (researchContext.Present && widgets.PropertyChip.Click != null) {
                    MaterialPropertyLabel captured = check.Label;
                    pools.PropertyChipClickHandler = () => ResearchWikiInputUtility.HandlePropertyChipClick(captured);
                    widgets.PropertyChip.Click.onClick.Register(pools.PropertyChipClickHandler);
                }
            }

            // 3. Body text lives inside the property group, so default
            // pages' BodyText stays untouched.
            if (widgets.PropertyBodyText != null) {
                widgets.PropertyBodyText.text = page.Body ?? " ";
            }

            // 4. Decomposed observations.
            if (widgets.PropertyLeafChipContainer != null) {
                bool leafChipsClickable = researchContext.Present && ArePropertyLeafChipsClickable();
                DecomposeFirstDefinition(check.Label);

                for (int i = 0; i < s_LeafScratch.Count; i++) {
                    MaterialObservationEntry leaf = s_LeafScratch[i];
                    ResearchObservationChip chip = pools.ChipPool.Alloc();
                    if (chip == null) {
                        break;
                    }
                    chip.transform.SetParent(widgets.PropertyLeafChipContainer, false);

                    string text = researchContext.Present
                        ? ResearchWikiInputUtility.GetObservationChipText(leaf.Label, researchContext.InterfacerState)
                        : MaterialPropertyLabelDisplay.GetObservationName(leaf.Label);
                    chip.SetState(text, ChipFillState.Filled, false, leaf.ObservationType);
                    chip.SetPickerChipDisabledVisual(false);

                    // Addressable from Leaf as "wiki:property-conductor-conductive". Keyed by
                    // (property, observation) rather than by column position, so a script keeps
                    // naming the same chip when the decomposition gains or loses a leaf.
                    WikiElementTagUtility.Stamp(chip, WikiElementTagUtility.PropertyObservationId(check.Label, leaf.Label));

                    Action handler = null;
                    if (leafChipsClickable && chip.Click != null) {
                        MaterialPropertyLabel captured = leaf.Label;
                        handler = () => ResearchWikiInputUtility.HandleObservationChipClick(captured);
                        chip.Click.onClick.Register(handler);
                    }

                    pools.ActivePropertyLeafChips.Add(chip);
                    pools.ActivePropertyLeafClickHandlers.Add(handler);
                }
            }

            // 5. Lay out + resize the group.
            float contentHeight = ResearchUILayoutUtility.LayoutVerticalAlignedTop(
                widgets.PropertyLeafChipContainer, pools.ActivePropertyLeafChips, pools.ActivePropertyLeafChips.Count, ChipGap);
            /*
            if (widgets.PropertyLeafChipContainer != null) {
                RectTransform groupRect = widgets.PropertyLeafChipContainer.transform as RectTransform;
                if (groupRect != null) {
                    Vector2 size = groupRect.sizeDelta;
                    size.y = contentHeight + 2f * OverlayPadding;
                    groupRect.sizeDelta = size;
                }
            }
            */
        }

        // Fills s_LeafScratch with the label's first-registered definition's
        // leaves. A property with no registered definition yields no chips —
        // a content gap, so it warns rather than failing silently.
        private static void DecomposeFirstDefinition(MaterialPropertyLabel label) {
            s_LeafScratch.Clear();

            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();
            if (registry == null) {
                return;
            }
            MaterialPropertyDefinition[] defs = registry.GetDefinitions(label);
            if (defs.Length == 0) {
                Debug.LogWarningFormat("[WikiPropertyLoadUtility] No MaterialPropertyDefinition registered for property '{0}'.", label);
                return;
            }
            MaterialPropertyDefinitionUtility.DecomposeToObservations(defs[0], s_NullContext, s_LeafScratch);
        }

        private static bool ArePropertyLeafChipsClickable() {
            ResearchUIAssets uiAssets = Find.GlobalAsset<ResearchUIAssets>();
            return uiAssets != null && uiAssets.PropertyPageObservationChipsClickable;
        }

        // Returns every pool-held leaf chip to the pool and unbinds the
        // authored property chip, deregistering handlers and onboarding tag
        // ids in lockstep. Called as the first step of LoadFor (clean slate),
        // and directly when navigating away from a property page.
        public static void FreeAllPropertyChips(WikiPageContentWidgets widgets, WikiChipPools pools) {
            if (pools == null) return;

            if (widgets != null && widgets.PropertyChip != null) {
                if (widgets.PropertyChip.Click != null && pools.PropertyChipClickHandler != null) {
                    widgets.PropertyChip.Click.onClick.Deregister(pools.PropertyChipClickHandler);
                }
                WikiElementTagUtility.Clear(widgets.PropertyChip);
            }
            pools.PropertyChipClickHandler = null;

            if (pools.ActivePropertyLeafChips == null) return;
            int n = pools.ActivePropertyLeafChips.Count;
            for (int i = n - 1; i >= 0; i--) {
                ResearchObservationChip chip = pools.ActivePropertyLeafChips[i];
                Action handler = i < pools.ActivePropertyLeafClickHandlers.Count
                    ? pools.ActivePropertyLeafClickHandlers[i]
                    : null;
                if (chip != null && chip.Click != null && handler != null) {
                    chip.Click.onClick.Deregister(handler);
                }
                if (chip != null) {
                    WikiElementTagUtility.Clear(chip);
                    Pool.TryFree(chip);
                }
            }
            pools.ActivePropertyLeafChips.Clear();
            pools.ActivePropertyLeafClickHandlers?.Clear();
        }
    }
}

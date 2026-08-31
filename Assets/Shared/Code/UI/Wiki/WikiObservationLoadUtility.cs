using BeauPools;
using BeauUtil;
using SpaceFab.Materials;
using SpaceFab.Research;
using System;
using UnityEngine;
using static SpaceFab.Title.TitleController;

namespace SpaceFab.UI {
    /// <summary>
    /// Rebuilds the chip column on a wiki observation page: one chip per
    /// observation label of the page's ObservationType, in enum order.
    /// Mirrors WikiCharacteristicsLoadUtility's shape — free prior chips,
    /// alloc, lay out vertically, resize the group to fit.
    ///
    /// In the Research scene each chip carries a click that adds the
    /// observation to the sample panel, or removes it when it is already
    /// there (rendered greyed). Everywhere else the chips render plain and
    /// take no clicks at all, since there is no sample panel to feed.
    ///
    /// Called by WikiVisualsUtility.RefreshPageContent immediately before it
    /// shows the observation group, so the column is never rendered empty.
    /// </summary>
    public static class WikiObservationLoadUtility {
        // Padding (px) added above and below the chip column when resizing
        // ObservationGroup's RectTransform.
        private const float OverlayPadding = 16f;

        // Gap (px) between adjacent chips. Mirrors the material
        // characteristics column so the two read as one visual language.
        private const float ChipGap = 8f;

        // Frees any previously-allocated observation chips, then allocs one
        // per observation label matching the page's type. Chips render
        // filled — the page shows what the player can observe, not empty
        // slots — and grey when the observation is already in the sample
        // panel. Non-selected chips stay ungreyed even when the panel is
        // full; the click handler ignores them instead.
        public static void LoadFor(WikiPageContentWidgets widgets, WikiChipPools pools, WikiPageData page, in WikiResearchContext researchContext) {
            if (widgets == null || pools == null || pools.ChipPool == null) return;
            if (widgets.ObservationChipContainer == null) return;

            // 1. Free prior chips.
            FreeAllObservationChips(pools);

            // Observations already in the sample panel are matched under the
            // context the collect path stores them with — the substrate in
            // the doping chamber, nothing elsewhere.
            StringHash32 slotContext = researchContext.Present
                ? ResearchWikiInputUtility.GetActiveObservationContext(researchContext.InterfacerState)
                : StringHash32.Null;

            // 2. Walk the observation block in enum order. Everything from
            // ConductorNaive on is a persistent property, not an
            // observation, and never belongs on these pages.
            for (MaterialPropertyLabel label = 0; label < MaterialPropertyLabel.ConductorNaive; label++) {
                if (MaterialObservationChamberLookup.GetChamberType(label) != page.ObservationType) {
                    continue;
                }

                ResearchObservationChip chip = pools.ChipPool.Alloc();
                if (chip == null) {
                    break;
                }
                chip.transform.SetParent(widgets.ObservationChipContainer, false);

                string text = researchContext.Present
                    ? ResearchWikiInputUtility.GetObservationChipText(label, researchContext.InterfacerState)
                    : MaterialPropertyLabelDisplay.GetObservationName(label);
                chip.SetState(text, ChipFillState.Filled, false, page.ObservationType);

                WikiElementTagUtility.Stamp(chip, WikiElementTagUtility.ObservationTypeObservationId(page.ObservationType, label));

                // Capture the label rather than the index — the click
                // resolves its own slot, and the list order is only
                // bookkeeping for the free pass.
                Action handler = null;
                bool selected = false;
                if (researchContext.Present) {
                    selected = ResearchWikiInputUtility.FindSlotIndex(researchContext.ViewModel, label, slotContext) >= 0;

                    MaterialPropertyLabel captured = label;
                    handler = () => ResearchWikiInputUtility.HandleObservationChipClick(captured);
                    if (chip.Click != null) {
                        chip.Click.onClick.Register(handler);
                    }
                }
                chip.SetPickerChipDisabledVisual(selected);

                pools.ActiveObservationChips.Add(chip);
                pools.ActiveObservationLabels.Add(label);
                pools.ActiveObservationClickHandlers.Add(handler);
            }

            // 3. Lay out + resize the group.
            float contentHeight = ResearchUILayoutUtility.LayoutVerticalAlignedTop(
                widgets.ObservationChipContainer, pools.ActiveObservationChips, pools.ActiveObservationChips.Count, ChipGap);
            /*
            if (widgets.ObservationGroup != null) {
                RectTransform groupRect = widgets.ObservationGroup.transform as RectTransform;
                if (groupRect != null) {
                    Vector2 size = groupRect.sizeDelta;
                    size.y = contentHeight + 2f * OverlayPadding;
                    groupRect.sizeDelta = size;
                }
            }
            */
        }

        // Returns every pool-held observation chip to the pool,
        // deregistering its click handler in lockstep. Called as the first
        // step of LoadFor (clean slate), and directly when navigating away
        // from an observation page so chips don't stay parked under the
        // container with live handlers.
        public static void FreeAllObservationChips(WikiChipPools pools) {
            if (pools == null || pools.ActiveObservationChips == null) return;
            int n = pools.ActiveObservationChips.Count;
            for (int i = n - 1; i >= 0; i--) {
                ResearchObservationChip chip = pools.ActiveObservationChips[i];
                Action handler = i < pools.ActiveObservationClickHandlers.Count
                    ? pools.ActiveObservationClickHandlers[i]
                    : null;
                if (chip != null && chip.Click != null && handler != null) {
                    chip.Click.onClick.Deregister(handler);
                }
                if (chip != null) {
                    Pool.TryFree(chip);
                }
            }
            pools.ActiveObservationChips.Clear();
            pools.ActiveObservationLabels?.Clear();
            pools.ActiveObservationClickHandlers?.Clear();
        }
    }
}

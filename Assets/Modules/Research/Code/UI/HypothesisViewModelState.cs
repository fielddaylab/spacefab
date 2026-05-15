using FieldDay.SharedState;
using System;

namespace SpaceFab.Research {
    /// <summary>
    /// Per-frame viewmodel polled by ResearchHypothesisPanel on LateUpdate.
    /// HypothesisViewModelSystem recomputes every field under ResearchMask
    /// from the current page list, the slotted material, and the sandbox /
    /// player-progress confirmation state. Cap of 32 leaf observations per
    /// page (well above the screenshot's three) since the satisfied mask is
    /// a uint; chips beyond that index would silently render unfilled.
    /// </summary>
    public class HypothesisViewModelState : SharedStateComponent {
        public const int MaxObservationsPerPage = 32;

        [NonSerialized] public int ActivePageIndex;
        [NonSerialized] public int ActivePageObservationCount;

        // Bit i = leaf i of the active page is satisfied (either the player
        // collected it directly, or an ancestor sub-property is confirmed
        // for the slotted material). Drives the colored chip fill on both
        // the hypothesis panel and the sample panel.
        [NonSerialized] public uint ActivePageSatisfiedMask;

        // Bit i = leaf i is satisfied via an ancestor sub-property already
        // being confirmed for the slotted material (auto-populated). A
        // subset of ActivePageSatisfiedMask. The sample panel renders these
        // slots as non-removable.
        [NonSerialized] public uint ActivePageLockedMask;

        [NonSerialized] public int ActivePageSatisfiedCount;

        // True when any material the player knows of (sandbox or saved
        // PlayerProgress) has this page's (Label, Context) confirmed.
        // Drives the header checkmark.
        [NonSerialized] public bool ActivePageIsFulfilled;

        // True when the slotted material has satisfied every chip on the
        // active page. Drives the submit button's visibility.
        [NonSerialized] public bool SubmitButtonVisible;

        [NonSerialized] public bool HypothesisChangedThisFrame;

        // Rebuild request flag. Other systems set this via
        // HypothesisViewModelUtility.RequestRebuild when their work
        // invalidates the viewmodel (observation add/remove, hypothesis
        // confirmation, page-list rebuild). HypothesisViewModelSystem
        // clears it once it has recomputed. The cycle-delta and the
        // ChamberInterfacerState.SlotMaterialUpdatedThisFrame flag are
        // checked separately as additional rebuild triggers, so callers
        // that already raise those frame-flags do not also need to call
        // RequestRebuild.
        [NonSerialized] public bool NeedsRebuild;
    }
}

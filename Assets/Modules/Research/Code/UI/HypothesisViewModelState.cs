using BeauUtil;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;

namespace SpaceFab.Research {
    /// <summary>
    /// Per-frame viewmodel polled by the research UI on LateUpdate.
    /// HypothesisViewModelSystem recomputes every field under ResearchMask
    /// from the current page list, the slotted material, and the sandbox /
    /// player-progress confirmation state.
    ///
    /// Two parallel views of the active page:
    /// - **Leaf view** (LeafSatisfiedMask / LeafLockedMask): one bit per
    ///   leaf in pagesState.Pages[ActivePageIndex].DecomposedObservations.
    ///   Drives the *hypothesis panel*, which shows the hypothesis's
    ///   required observations. Bit i set = leaf i is satisfied by
    ///   *some* slot entry (or by ancestor-confirmed property).
    /// - **Slot view** (SlotLabels / SlotContexts / SlotCount /
    ///   SlotLockedMask): the ordered list of observations occupying
    ///   the sample panel's N slots. Auto-locked entries (from ancestor-
    ///   confirmed properties) come first; player-picked entries follow
    ///   in insertion order. Drives the *sample panel* chips. Slot count
    ///   cap is ActivePageObservationCount (= leaf count). Player picks
    ///   that don't match any leaf still occupy a slot.
    ///
    /// Cap of 32 leaves / slots per page since the masks are uints;
    /// chips beyond bit 31 silently fall off.
    /// </summary>
    public class HypothesisViewModelState : SharedStateComponent, FieldDay.IRegistrationCallbacks {
        public const int MaxObservationsPerPage = 3;

        [NonSerialized] public int ActivePageIndex;

        // Capacity for the slot view; equals the active page's leaf count
        // (clamped to MaxObservationsPerPage). Also bounds the slot
        // arrays' meaningful range.
        [NonSerialized] public int ActivePageObservationCount;

        // === Leaf view ===

        // Bit i = leaf i of the active page is satisfied by some slot
        // entry or an ancestor-confirmed property. Drives the hypothesis
        // panel chip fill.
        [NonSerialized] public uint ActivePageLeafSatisfiedMask;

        // Bit i = leaf i is auto-satisfied via an ancestor-confirmed
        // sub-property (player didn't have to pick it). Subset of
        // ActivePageLeafSatisfiedMask. Drives hypothesis panel locked
        // overlays.
        [NonSerialized] public uint ActivePageLeafLockedMask;

        [NonSerialized] public int ActivePageLeafSatisfiedCount;

        // === Slot view ===

        // Filled slots in display order. Index [0..ActivePageSlotCount)
        // is valid. Auto-locked entries (ancestor-confirmed) come first;
        // player picks follow in insertion order.
        [NonSerialized] public MaterialPropertyLabel[] ActivePageSlotLabels;
        [NonSerialized] public StringHash32[] ActivePageSlotContexts;

        [NonSerialized] public int ActivePageSlotCount;

        // Bit i = slot i is locked (auto-confirmed, non-removable).
        [NonSerialized] public uint ActivePageSlotLockedMask;

        // === Page paginator ===

        // Bit i = page i has been fulfilled by some known material.
        // Drives the per-dot "confirmed" overlay on the paginator.
        [NonSerialized] public uint PageFulfilledMask;

        // === Submit ===

        // True when every leaf on the active page is satisfied. Drives
        // the submit button's visibility.
        [NonSerialized] public bool SubmitButtonVisible;

        // === Frame flags ===

        [NonSerialized] public bool HypothesisChangedThisFrame;

        // Rebuild request flag. Other systems set this via
        // HypothesisViewModelUtility.RequestRebuild when their work
        // invalidates the viewmodel. HypothesisViewModelSystem clears it
        // once it has recomputed.
        [NonSerialized] public bool NeedsRebuild;

        public void OnRegister() {
            ActivePageSlotLabels = new MaterialPropertyLabel[MaxObservationsPerPage];
            ActivePageSlotContexts = new StringHash32[MaxObservationsPerPage];
        }

        public void OnDeregister() {
        }
    }
}

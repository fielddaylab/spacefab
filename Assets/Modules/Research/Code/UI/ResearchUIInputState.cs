using BeauUtil;
using FieldDay.SharedState;
using SpaceFab.Materials;
using System;

namespace SpaceFab.Research {
    /// <summary>
    /// Frame-flagged input from Research UI MonoBehaviours. Click handlers
    /// Frame-flagged input from Research UI MonoBehaviours. Click handlers
    /// write their intent here; UI-driven systems consume the flags during
    /// Update; ResearchUIInputRefreshSystem clears them at end-of-frame.
    /// Replaces the prototype's per-MonoBehaviour CastableEvent listeners.
    /// </summary>
    public class ResearchUIInputState : SharedStateComponent {
        // True for one frame after the sample panel's "ADD OBSERVATION +"
        // button is clicked. ResearchSamplePanel toggles its picker overlay
        // on; no system reads this flag (it's UI-internal), but lives here
        // so the refresh system clears it.
        [NonSerialized] public bool AddObservationClickedThisFrame;

        // Set when an observation chip emits an add request (wiki
        // observation / property pages, or the dormant picker overlay).
        // ObservationCollectSystem consumes the pair and calls
        // ResearchInventoryUtility.AddObservation against the slotted
        // material.
        [NonSerialized] public MaterialPropertyLabel ChipPickerSelectionLabel;
        [NonSerialized] public bool ChipPickerSelectedThisFrame;

        // Set when the player clicks a non-locked filled slot in the
        // observations panel, or a greyed (already-selected) wiki
        // observation chip. The index points into the viewmodel's slot
        // view; ObservationCollectSystem resolves it to (label, context)
        // and removes that entry from the slotted material's observation
        // list.
        [NonSerialized] public int RemoveObservationSlotIndex;
        [NonSerialized] public bool RemoveObservationClickedThisFrame;

        // Set when a wiki property chip requests hypothesis selection.
        // HypothesisViewModelSystem validates and applies the label.
        [NonSerialized] public MaterialPropertyLabel AddHypothesisLabel;
        [NonSerialized] public bool HypothesisSelectedClickedThisFrame;
        [NonSerialized] public bool RemoveHypothesisClickedThisFrame;

        // Set when the hypothesis panel's submit button is clicked.
        // HypothesisSubmitSystem consumes it.
        [NonSerialized] public bool VerifyHypothesisClickedThisFrame;
    }
}

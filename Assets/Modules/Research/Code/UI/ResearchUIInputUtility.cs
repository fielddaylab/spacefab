using BeauUtil;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Mutation helpers for ResearchUIInputState. View MonoBehaviours route
    /// every click here so the state class stays data-only and every input
    /// transition has one named seam (easier to grep, easier to log later).
    /// All helpers are no-op when state is null so caller don't have to
    /// guard every site.
    /// </summary>
    public static class ResearchUIInputUtility {
        // Sample panel's ADD OBSERVATION + button. The view tracks the
        // picker-open state itself; this flag is for telemetry / refresh.
        public static void RequestAddObservation(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.AddObservationClickedThisFrame = true;
        }

        // Player picked a chip from the chamber's available-observations
        // picker. ObservationCollectSystem consumes the (label, slottedId)
        // pair next Update.
        public static void RequestPickerSelection(ResearchUIInputState inputState, MaterialPropertyLabel label) {
            if (inputState == null) return;
            inputState.ChipPickerSelectionLabel = label;
            inputState.ChipPickerSelectedThisFrame = true;
        }

        // Player clicked a filled, non-locked observation slot in the
        // sample panel or a greyed wiki observation chip.
        // ObservationCollectSystem resolves the slot index to a (label,
        // context) via the viewmodel's slot view.
        public static void RequestRemoveObservation(ResearchUIInputState inputState, int slotIndex) {
            if (inputState == null) return;
            inputState.RemoveObservationSlotIndex = slotIndex;
            inputState.RemoveObservationClickedThisFrame = true;
        }

        // Player clicked a wiki property chip to select it as the active
        // hypothesis. HypothesisViewModelSystem validates (dynamic
        // context, fulfilled rejection) and applies the label on its
        // next rebuild.
        public static void RequestHypothesisSelection(ResearchUIInputState inputState, MaterialPropertyLabel label) {
            if (inputState == null) return;
            inputState.AddHypothesisLabel = label;
            inputState.HypothesisSelectedClickedThisFrame = true;
        }

        // Player clicked the filled hypothesis slot in the sample panel
        // or the greyed property chip on its wiki page.
        // ObservationCollectSystem clears the viewmodel's selection.
        public static void RequestRemoveHypothesis(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.RemoveHypothesisClickedThisFrame = true;
        }

        // Player clicked the hypothesis-panel submit button.
        // HypothesisSubmitSystem consumes the flag next Update.
        public static void RequestSubmit(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.VerifyHypothesisClickedThisFrame = true;
        }

        // End-of-frame clear. ResearchUIInputRefreshSystem calls this from
        // its ProcessWork.
        public static void ClearFrameFlags(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.AddObservationClickedThisFrame = false;
            inputState.ChipPickerSelectedThisFrame = false;
            inputState.RemoveObservationClickedThisFrame = false;
            inputState.RemoveObservationSlotIndex = -1;
            inputState.VerifyHypothesisClickedThisFrame = false;
            inputState.RemoveHypothesisClickedThisFrame = false;
            inputState.HypothesisSelectedClickedThisFrame = false;
        }
    }
}

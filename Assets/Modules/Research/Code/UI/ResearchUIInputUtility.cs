using SpaceFab.Materials;

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
        // sample panel. ObservationCollectSystem resolves the slot index
        // to a (label, context) via the active hypothesis page.
        public static void RequestRemoveObservation(ResearchUIInputState inputState, int slotIndex) {
            if (inputState == null) return;
            inputState.RemoveObservationSlotIndex = slotIndex;
            inputState.RemoveObservationClickedThisFrame = true;
        }

        // Player clicked a filled hypothesis slot in the
        // sample panel. ObservationCollectSystem resolves the slot index
        // to a (label, context) via the active hypothesis page.
        public static void RequestRemoveHypothesis(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.RemoveHypothesisClickedThisFrame = true;
        }

        // Player clicked the hypothesis-panel submit button.
        // HypothesisSubmitSystem consumes the flag next Update.
        public static void RequestSubmit(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.SubmitHypothesisClickedThisFrame = true;
        }

        // End-of-frame clear. ResearchUIInputRefreshSystem calls this from
        // its ProcessWork.
        public static void ClearFrameFlags(ResearchUIInputState inputState) {
            if (inputState == null) return;
            inputState.AddObservationClickedThisFrame = false;
            inputState.ChipPickerSelectedThisFrame = false;
            inputState.RemoveObservationClickedThisFrame = false;
            inputState.RemoveObservationSlotIndex = -1;
            inputState.SubmitHypothesisClickedThisFrame = false;
        }
    }
}

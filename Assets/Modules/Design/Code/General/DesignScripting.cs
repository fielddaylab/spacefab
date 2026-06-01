using System;
using FieldDay;
using Leaf.Runtime;
using SpaceFab.Save;

namespace SpaceFab.Design {
    /// <summary>
    /// Leaf-callable queries and commands specific to the Design minigame.
    /// </summary>
    public static class DesignScripting {
        // Returns the zero-based index of the Design level the player is currently working on
        // within the active contract. Used by onboarding scripts to gate beats on the first level.
        // Returns 0 when no Design minigame is active.
        [LeafMember("CurrLevelIndex")]
        public static int Leaf_CurrLevelIndex() {
            if (!Game.SharedState.Has<DesignMinigameState>()) { return 0; }
            return Find.State<DesignMinigameState>().ActiveLevelIndex;
        }

        // Returns whether the Design level at the given index has been solved. Returns false when
        // no save state is present or the index is out of range for the active contract.
        [LeafMember("IsLevelSolved")]
        public static bool Leaf_IsLevelSolved(int levelIndex) {
            if (!Game.SharedState.Has<MinigameSaveStates>()) { return false; }
            DesignSaveState designSaveState = Find.State<MinigameSaveStates>().Design;
            if (levelIndex < 0 || levelIndex >= designSaveState.LevelCount) { return false; }
            return designSaveState.FoundValidSolutionForLevel[levelIndex];
        }

        // Whether the input node labelled `inputLabel` (e.g. "A", "B", "IN") has a physical path on
        // the grid to ANY output node. Connectivity only — ignores transistor gating / signal logic.
        // Returns false when no Design grid is present or the label is unrecognized.
        [LeafMember("IsInputConnectedToAnyOutput")]
        public static bool Leaf_IsInputConnectedToAnyOutput(string inputLabel) {
            if (!Game.SharedState.Has<GridStackState>()) { return false; }
            if (!TryParseLabel(inputLabel, out InputOutputNodeTypeFlags input)) { return false; }
            return GridConnectivityUtility.IsInputConnectedToAnyOutput(Find.State<GridStackState>(), input);
        }

        // Whether the input node labelled `inputLabel` (e.g. "A") has a physical path on the grid to
        // the output node labelled `outputLabel` (e.g. "X", "Y", "Z", "OUT"). Connectivity only.
        // Returns false when no Design grid is present or either label is unrecognized.
        [LeafMember("IsInputConnectedToOutput")]
        public static bool Leaf_IsInputConnectedToOutput(string inputLabel, string outputLabel) {
            if (!Game.SharedState.Has<GridStackState>()) { return false; }
            if (!TryParseLabel(inputLabel, out InputOutputNodeTypeFlags input)) { return false; }
            if (!TryParseLabel(outputLabel, out InputOutputNodeTypeFlags output)) { return false; }
            return GridConnectivityUtility.IsInputConnectedToOutput(Find.State<GridStackState>(), input, output);
        }

        // Number of test rows whose verdict is currently Correct. Reflects the live per-row results
        // accumulated across the current run (toggle-input mode resolves one row per Test click;
        // full-suite resolves all). Returns 0 when no simulate run state is present.
        [LeafMember("PassingRowCount")]
        public static int Leaf_PassingRowCount() {
            if (!Game.SharedState.Has<SimulateRunState>()) { return 0; }
            TestRowVerdict[] verdicts = Find.State<SimulateRunState>().RowVerdicts;
            if (verdicts == null) { return 0; }

            int count = 0;
            for (int i = 0; i < verdicts.Length; i++) {
                if (verdicts[i] == TestRowVerdict.Correct) { count++; }
            }
            return count;
        }

        // Whether any input node labelled `inputLabel` (e.g. "A", "B", "IN") is currently toggled to
        // `state` ("Hi"/"High" or "Lo"/"Low"). Returns false when no toggle state is present or
        // either argument is unrecognized. Matches any cell carrying the label (mirrors the
        // any-cell-with-label semantics of the connectivity hooks).
        [LeafMember("IsInputInState")]
        public static bool Leaf_IsInputInState(string inputLabel, string state) {
            if (!Game.SharedState.Has<InputToggleState>()) { return false; }
            if (!TryParseLabel(inputLabel, out InputOutputNodeTypeFlags label)) { return false; }
            if (!TryParseFlowState(state, out FlowState wantState)) { return false; }

            InputToggleState toggleState = Find.State<InputToggleState>();
            if (toggleState.Inputs == null) { return false; }
            for (int i = 0; i < toggleState.InputCount; i++) {
                InputToggleEntry entry = toggleState.Inputs[i];
                if ((entry.Subtype & label) != 0 && entry.State == wantState) { return true; }
            }
            return false;
        }

        // Maps a designer-facing toggle-state string to FlowState, case-insensitively. Accepts the
        // short forms ("Hi"/"Lo") and the spelled-out forms ("High"/"Low"). Only the two binary
        // toggle states are valid here — Empty / Unstable aren't player-selectable.
        private static bool TryParseFlowState(string state, out FlowState result) {
            result = default;
            if (string.IsNullOrEmpty(state)) { return false; }

            switch (state.Trim().ToUpperInvariant()) {
                case "HI":
                case "HIGH": result = FlowState.Hi; return true;
                case "LO":
                case "LOW": result = FlowState.Lo; return true;
            }
            return false;
        }

        // Maps a designer-facing node label to its InputOutputNodeTypeFlags value, case-insensitively.
        // Accepts the short output forms ("X"/"Y"/"Z") as well as the enum names ("OUTX"/"OUTY"/"OUTZ").
        // Input labels ("A"/"B"/"C"/"IN") and rails ("VPLUS"/"VMINUS") parse by their enum name.
        private static bool TryParseLabel(string label, out InputOutputNodeTypeFlags result) {
            result = default;
            if (string.IsNullOrEmpty(label)) { return false; }

            switch (label.Trim().ToUpperInvariant()) {
                case "X": result = InputOutputNodeTypeFlags.OUTX; return true;
                case "Y": result = InputOutputNodeTypeFlags.OUTY; return true;
                case "Z": result = InputOutputNodeTypeFlags.OUTZ; return true;
            }

            return Enum.TryParse(label.Trim(), true, out result);
        }
    }
}

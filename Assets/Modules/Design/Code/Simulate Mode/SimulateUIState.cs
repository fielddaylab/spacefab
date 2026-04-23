using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// View-side state for the evaluation table UI in Simulate mode. The phase machine in
    /// SimulateModeSystem drives TableBuilt / ResultsPanelVisible / HighlightedRowIndex /
    /// UnstableBannerVisible; SimulateUIUtility performs the actual row/cell writes when the
    /// view references are populated.
    /// </summary>
    public class SimulateUIState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool TableBuilt;
        [HideInInspector] public bool ResultsPanelVisible;
        [HideInInspector] public int HighlightedRowIndex;
        [HideInInspector] public bool UnstableBannerVisible;

        // TODO: references to row/cell view handles (SuiteRow / SuiteContents / SuiteCellEval),
        //       populated when UI construction is ported from EvaluationMgr.ConstructSuiteTable.

        public void OnRegister()
        {
            TableBuilt = false;
            ResultsPanelVisible = false;
            HighlightedRowIndex = -1;
            UnstableBannerVisible = false;
        }

        public void OnDeregister()
        {
        }
    }

    /// <summary>
    /// View-side helpers for the evaluation table. Called by SimulateModeSystem phase handlers
    /// at row-start, row-resolve, suite-complete, dismiss, and cancel points. Bodies to be ported
    /// from the prototype's EvaluationMgr UI Construction region.
    /// </summary>
    public static class SimulateUIUtility
    {
        // Constructs the header row + one content row per test. Called once on Simulate entry.
        // Ported from EvaluationMgr.ConstructSuiteTable.
        public static void BuildTable(SimulateUIState uiState, TestSuiteData suite)
        {
            // TODO: instantiate header prefabs + contents prefabs per test row;
            //       instantiate SuiteCellEval overlays on output columns.
            //       Store view handles on uiState. Set TableBuilt = true.
        }

        // Disables every eval image and flow image on the table. Ported from EvaluationMgr.ClearSuiteEvals.
        public static void ClearAllEvalMarks(SimulateUIState uiState)
        {
            // TODO: for each SuiteCellEval in the table, disable Img.
            // TODO: for each SuiteContents in the table, disable FlowImg.
        }

        // Writes the input values for a row into the contents cells (skipping output columns).
        // Called at the start of each test during PreparingTest.
        public static void WriteRowInputs(SimulateUIState uiState, int rowIndex, TestData test)
        {
            // TODO: for each non-output column in row rowIndex, look up the value in test.Bundle
            //       by its InputOutputNodeTypeFlags and set the contents cell's FlowImg sprite.
        }

        // Writes the verdict + per-output flow results for a row. Called during ResolvingTest.
        public static void WriteRowVerdict(SimulateUIState uiState, int rowIndex, TestRowVerdict verdict, FlowState[] outputFlows)
        {
            // TODO: for each output column in row rowIndex, enable SuiteCellEval.Img and
            //       SetCorrect / SetIncorrect based on verdict + per-output match. Update the
            //       contents FlowImg to the output's resulting sprite (Hi / Lo / Unstable).
        }

        // Shows the overall results panel. Called when entering SuiteComplete.
        public static void ShowResultsPanel(SimulateUIState uiState, bool allCorrect)
        {
            // TODO: activate the results panel GameObject; set header text to Success / Failure.
            //       Set ResultsPanelVisible = true.
        }

        // Hides the results panel. Called on Dismiss or on a fresh Play from SuiteComplete.
        public static void HideResultsPanel(SimulateUIState uiState)
        {
            // TODO: deactivate the results panel GameObject. Set ResultsPanelVisible = false.
        }
    }
}

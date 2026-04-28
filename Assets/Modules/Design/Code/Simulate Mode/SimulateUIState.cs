using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using SpaceFab.Design.Visuals;

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
        #region Inspector

        public RectTransform TableRect;
        public VerticalLayoutGroup VertLayout;

        #endregion // Inspector

        [HideInInspector] public bool TableBuilt;
        [HideInInspector] public bool ResultsPanelVisible;
        [HideInInspector] public int HighlightedRowIndex;
        [HideInInspector] public bool UnstableBannerVisible;

        // TODO: references to row/cell view handles (SuiteRow / SuiteCell / SuiteCellEval),
        //       populated when UI construction is ported from EvaluationMgr.ConstructSuiteTable.
        [HideInInspector] public SuiteRow[] Rows;

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
        public static void BuildTable(SimulateUIState uiState, TestSuiteData suite, SimulateRunState runState, SuiteVisualsDB suiteDB)
        {
            // TODO: instantiate SuiteCellEval overlays on output columns.

            // instantiate headers and size table
            SizeTable(uiState, suite, suiteDB);

            // instantiate rows and cols
            CreateRowsAndCols(uiState, suite, suiteDB);

            // hook into run state for play, pause, rewind, etc.
            AssignRunListeners(uiState, suite, runState);

            uiState.TableBuilt = true;
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

        #region Helpers

        private static void SizeTable(SimulateUIState uiState, TestSuiteData suite, SuiteVisualsDB suiteDB)
        {
            if (suite.Tests.Length == 0) { return; }

            var numCols = suite.Tests[0].Bundle.Length;
            float tableWidth = 0;
            bool inOutputPhase = false;

            // headers
            SuiteRow currRow = GameObject.Instantiate(suiteDB.RowPrefab, uiState.VertLayout.transform).GetComponent<SuiteRow>();
            currRow.RunButton.gameObject.SetActive(false);
            for (int i = 0; i < numCols; i++)
            {
                if (suite.Tests[0].Bundle[i].Id >= InputOutputNodeTypeFlags.OUT)
                {
                    if (!inOutputPhase)
                    {
                        // instantiate arrow image at input-output threshold (but hide image)
                        var arrowCol = GameObject.Instantiate(suiteDB.ArrowColPrefab, currRow.HorizontalLayout.transform).GetComponent<SuiteCol>();
                        arrowCol.FlowImg.enabled = false;
                        arrowCol.Label.enabled = false;
                        inOutputPhase = true;
                    }
                }

                SuiteHeader currHeader = GameObject.Instantiate(suiteDB.HeaderPrefab, currRow.HorizontalLayout.transform).GetComponent<SuiteHeader>();
                currHeader.Label.SetText(GetLocTextForId(suite.Tests[0].Bundle[i].Id));
                var size = currHeader.Rect.sizeDelta;
                size.x = suiteDB.InputColPrefab.GetComponent<RectTransform>().sizeDelta.x;
                currHeader.Rect.sizeDelta = size;
                tableWidth += currHeader.Rect.sizeDelta.x + currRow.HorizontalLayout.spacing;
            }

            // add width for arrow col
            tableWidth += suiteDB.ArrowColPrefab.GetComponent<RectTransform>().sizeDelta.x;

            int numRows = suite.Tests.Length;

            float margin = 10;
            Vector2 tableSize = uiState.TableRect.sizeDelta;
            tableSize.x = tableWidth + margin * 2;
            tableSize.y = suiteDB.HeaderPrefab.GetComponent<RectTransform>().sizeDelta.y
                + suiteDB.RowPrefab.GetComponent<RectTransform>().sizeDelta.y * numRows
                + (uiState.VertLayout.spacing * numRows)
                + margin * 2;
            uiState.TableRect.sizeDelta = tableSize;
        }

        private static void CreateRowsAndCols(SimulateUIState uiState, TestSuiteData suite, SuiteVisualsDB suiteDB)
        {
            uiState.Rows = new SuiteRow[suite.Tests.Length];
            bool inOutputPhase = false;
            for (int row = 0; row < suite.Tests.Length; row++)
            {
                // instantiate row
                uiState.Rows[row] = GameObject.Instantiate(suiteDB.RowPrefab, uiState.VertLayout.transform).GetComponent<SuiteRow>();
                uiState.Rows[row].Cols = new SuiteCol[suite.Tests[row].Bundle.Length];
                inOutputPhase = false;
                for (int col = 0; col < suite.Tests[row].Bundle.Length; col++)
                {
                    var bundle = suite.Tests[row].Bundle;
                    SuiteCol newCol;
                    if (bundle[col].Id < InputOutputNodeTypeFlags.OUT)
                    {
                        // input
                        newCol = GameObject.Instantiate(suiteDB.InputColPrefab, uiState.Rows[row].HorizontalLayout.transform).GetComponent<SuiteCol>();

                        // configure with flow visual
                        newCol.FlowImg.sprite = SuiteVisualsDBUtility.LookupSuiteColSprite(suiteDB, bundle[col].State);
                    }
                    else
                    {
                        if (!inOutputPhase)
                        {
                            // instantiate arrow image at input-output threshold
                            var arrowCol = GameObject.Instantiate(suiteDB.ArrowColPrefab, uiState.Rows[row].HorizontalLayout.transform).GetComponent<SuiteCol>();
                            arrowCol.FlowImg.sprite = SuiteVisualsDBUtility.LookupSuiteColSprite(suiteDB, bundle[col].State, isArrow: true);
                            arrowCol.Label.enabled = false;

                            inOutputPhase = true;
                        }

                        // output
                        newCol = GameObject.Instantiate(suiteDB.OutputColPrefab, uiState.Rows[row].HorizontalLayout.transform).GetComponent<SuiteCol>();

                        // configure table visual
                        newCol.FlowImg.sprite = SuiteVisualsDBUtility.LookupSuiteColSprite(suiteDB, bundle[col].State, isOutput: true);
                    }

                    newCol.Label.SetText(GetLocTextForFlow(bundle[col].State));

                    uiState.Rows[row].Cols[col] = newCol;
                }
            }
        }

        private static void AssignRunListeners(SimulateUIState uiState, TestSuiteData suite, SimulateRunState runState)
        {
            for (int row = 0; row < suite.Tests.Length; row++)
            {
                uiState.Rows[row].RunButton.onClick.AddListener(() =>
                {
                    // TODO: hook into run state
                    // runState.
                });
            }
        }

        // TODO: hook up with Loc system
        private static string GetLocTextForId(InputOutputNodeTypeFlags id)
        {
            if ((id & InputOutputNodeTypeFlags.IN) != 0) { return "In"; }
            else if ((id & InputOutputNodeTypeFlags.A) != 0) { return "In A"; }
            else if ((id & InputOutputNodeTypeFlags.B) != 0) { return "In B"; }
            else if ((id & InputOutputNodeTypeFlags.C) != 0) { return "In C"; }
            else if ((id & InputOutputNodeTypeFlags.OUT) != 0) { return "Out"; }
            else if ((id & InputOutputNodeTypeFlags.OUTX) != 0) { return "Out X"; }
            else if ((id & InputOutputNodeTypeFlags.OUTY) != 0) { return "Out Y"; }
            else if ((id & InputOutputNodeTypeFlags.OUTZ) != 0) { return "Out Z"; }

            return string.Empty;
        }

        // TODO: hook up with Loc system
        private static string GetLocTextForFlow(FlowState flow)
        {
            switch (flow)
            {
                case FlowState.Empty: return "--";
                case FlowState.Lo: return "Lo";
                case FlowState.Hi: return "Hi";
                case FlowState.Unstable: return "Unstable";
                default: return string.Empty;
            }
        }

        #endregion // Helpers
    }
}

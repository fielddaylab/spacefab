using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using SpaceFab.UI;
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

        // Suite-level run controls. Positioning in the SimTable hierarchy is TBD; the refs here
        // are wired in inspector once the prefab layout is finalized. Until then refs may be null —
        // the suite refresh systems guard against that.
        public SuiteRunButton SuiteRunButton;
        public DynamicButton SuiteRestartButton;
        public DynamicButton SuiteCancelButton;

        // Toggle-input mode "Test" button. Visible only when DesignMinigameState.UseToggleInputMode
        // is true; replaces the per-row + suite-run buttons. SuiteTestButtonRefreshSystem owns its
        // interactable state and hides it otherwise.
        public SuiteTestButton SuiteTestButton;

        #endregion // Inspector

        [HideInInspector] public bool TableBuilt;
        [HideInInspector] public bool ResultsPanelVisible;
        [HideInInspector] public int HighlightedRowIndex;
        [HideInInspector] public bool UnstableBannerVisible;

        // Set true whenever the SuiteRunRowButton icons might need to change (Phase change,
        // CurrentRow change, table built, button clicked). Consumed and cleared by
        // SuiteRunRowButtonRefreshSystem. Any future site that mutates SimulateRunState.Phase
        // or CurrentRow must also raise this flag — use SimulateUIUtility.MarkAllRunButtonsDirty
        // so the suite-level dirty flag stays in sync.
        [HideInInspector] public bool RunButtonsNeedRefreshing;

        // Set true whenever the suite-level run / restart / cancel buttons need a repaint
        // (icon swap on SuiteRunButton, interactable toggle on Restart/Cancel). Consumed by
        // SuiteRunButtonRefreshSystem (icon) and SuiteSecondaryButtonRefreshSystem (interactable),
        // cleared by the latter. Always raised together with RunButtonsNeedRefreshing via
        // SimulateUIUtility.MarkAllRunButtonsDirty.
        [HideInInspector] public bool SuiteButtonsNeedRefreshing;

        // Per-row references to instantiated view handles. SuiteRow.Cols / SuiteRow.Verdicts
        // are sized at BuildTable time, parallel to the test suite's bundle structure.
        [HideInInspector] public SuiteRow[] Rows;

        // Per-row × per-col verdict display state. CellVerdicts[row][col] holds the desired UI
        // state for the VerdictVisualizer at Rows[row].Verdicts[col]. Sized in CreateRowsAndCols,
        // parallel to Rows[*].Cols. VerdictsNeedRefreshing flags the array dirty for the
        // VerdictVisualizerRefreshSystem to consume.
        [HideInInspector] public CellVerdict[][] CellVerdicts;
        [HideInInspector] public bool VerdictsNeedRefreshing;

        public void OnRegister()
        {
            TableBuilt = false;
            ResultsPanelVisible = false;
            HighlightedRowIndex = -1;
            UnstableBannerVisible = false;
            RunButtonsNeedRefreshing = false;
            SuiteButtonsNeedRefreshing = false;
            VerdictsNeedRefreshing = false;
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
        // Ported from EvaluationMgr.ConstructSuiteTable. designState gates the classic vs toggle-input
        // chrome: toggle mode hides every per-row Run button and the suite Run/Restart/Cancel trio,
        // surfacing only the single SuiteTestButton.
        public static void BuildTable(SimulateUIState uiState, TestSuiteData suite, SimulateRunState runState, DesignMinigameState designState, SuiteVisualsDB suiteDB)
        {
            // Per-row CellVerdicts state arrays mirror the per-row Cols/Verdicts arrays created
            // in CreateRowsAndCols. Size the outer array here so CreateRowsAndCols can fill in
            // each row's slot inline.
            uiState.CellVerdicts = new CellVerdict[suite.Tests.Length][];

            // instantiate headers and size table
            SizeTable(uiState, suite, suiteDB);

            // instantiate rows and cols
            CreateRowsAndCols(uiState, suite, suiteDB);

            // hook into run state for play, pause, rewind, etc.
            AssignRunListeners(uiState, suite, runState);
            AssignSuiteListeners(uiState, runState);

            ApplyModeChrome(uiState, designState);

            uiState.TableBuilt = true;

            // Trigger the initial icon + verdict paint on every row, plus the suite-level controls.
            MarkAllRunButtonsDirty(uiState);
            uiState.VerdictsNeedRefreshing = true;
        }

        // Hides classic per-row + suite-level buttons in toggle-input mode (and shows the Test
        // button); inverse in classic mode. Called once at BuildTable time. The refresh systems
        // also self-gate per-frame to handle a runtime UseToggleInputMode flip.
        private static void ApplyModeChrome(SimulateUIState uiState, DesignMinigameState designState)
        {
            bool toggleMode = designState != null && designState.UseToggleInputMode;

            if (uiState.Rows != null)
            {
                for (int r = 0; r < uiState.Rows.Length; r++)
                {
                    SuiteRow row = uiState.Rows[r];
                    if (row == null || row.RunButton == null) { continue; }
                    row.RunButton.gameObject.SetActive(!toggleMode);
                }
            }

            if (uiState.SuiteRunButton != null) { uiState.SuiteRunButton.gameObject.SetActive(!toggleMode); }
            if (uiState.SuiteRestartButton != null) { uiState.SuiteRestartButton.gameObject.SetActive(!toggleMode); }
            if (uiState.SuiteCancelButton != null) { uiState.SuiteCancelButton.gameObject.SetActive(!toggleMode); }
            if (uiState.SuiteTestButton != null) { uiState.SuiteTestButton.gameObject.SetActive(toggleMode); }
        }

        // Raises both the per-row and suite-level run-button dirty flags. Use this anywhere
        // SimulateRunState.Phase or CurrentRow changes — the row buttons and the suite buttons
        // both derive their state from those, and missing one drifts silently.
        public static void MarkAllRunButtonsDirty(SimulateUIState uiState)
        {
            uiState.RunButtonsNeedRefreshing = true;
            uiState.SuiteButtonsNeedRefreshing = true;
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

        // Records per-output verdict outcomes for a row into uiState.CellVerdicts and flags the
        // verdict visuals dirty. Called from ProcessResolvingTest after scoring. actualPerCol is
        // sized to currTest.Bundle.Length, indexed by bundle column; non-output columns are
        // skipped. State only — VerdictVisualizerRefreshSystem applies the sprites.
        public static void WriteRowVerdict(SimulateUIState uiState, int rowIndex, TestData currTest, FlowState[] actualPerCol)
        {
            if (uiState.CellVerdicts == null || rowIndex < 0 || rowIndex >= uiState.CellVerdicts.Length) { return; }
            CellVerdict[] verdicts = uiState.CellVerdicts[rowIndex];
            if (verdicts == null) { return; }

            int colCount = verdicts.Length;
            if (currTest.Bundle.Length < colCount) { colCount = currTest.Bundle.Length; }

            for (int col = 0; col < colCount; col++)
            {
                if (currTest.Bundle[col].Id < InputOutputNodeTypeFlags.OUT) { continue; }

                FlowState expected = currTest.Bundle[col].State;
                verdicts[col] = (actualPerCol[col] == expected) ? CellVerdict.Correct : CellVerdict.Incorrect;
            }

            uiState.VerdictsNeedRefreshing = true;
        }

        // Marks every output column across every row as Correct in uiState.CellVerdicts and
        // flags the verdict visuals dirty. Called from SimTableLoadSystem on Design entry when
        // the player has previously found a valid solution (DesignMinigameState.FoundValidSolution)
        // — the contract is already solved, so the suite reads as passing on entry instead of
        // requiring the player to re-run.
        public static void MarkAllRowsCorrect(SimulateUIState uiState, TestSuiteData suite)
        {
            if (uiState.CellVerdicts == null || suite == null || suite.Tests == null) { return; }

            int rowCount = uiState.CellVerdicts.Length;
            if (suite.Tests.Length < rowCount) { rowCount = suite.Tests.Length; }

            for (int row = 0; row < rowCount; row++)
            {
                CellVerdict[] verdicts = uiState.CellVerdicts[row];
                if (verdicts == null) { continue; }

                TestEntry[] bundle = suite.Tests[row].Bundle;
                int colCount = verdicts.Length;
                if (bundle.Length < colCount) { colCount = bundle.Length; }

                // Output columns get Correct; input columns stay Hidden — matches WriteRowVerdict's
                // skip rule so input cells aren't decorated with a verdict mark they shouldn't carry.
                for (int col = 0; col < colCount; col++)
                {
                    if (bundle[col].Id < InputOutputNodeTypeFlags.OUT) { continue; }
                    verdicts[col] = CellVerdict.Correct;
                }
            }

            uiState.VerdictsNeedRefreshing = true;
        }

        // Marks every output column in the given row as Hidden in uiState.CellVerdicts and
        // flags the verdict visuals dirty. Called from ProcessPreparingTest so a re-run doesn't
        // keep the previous run's verdict visible while the new propagation plays out.
        public static void HideRowVerdicts(SimulateUIState uiState, int rowIndex)
        {
            if (uiState.CellVerdicts == null || rowIndex < 0 || rowIndex >= uiState.CellVerdicts.Length) { return; }
            CellVerdict[] verdicts = uiState.CellVerdicts[rowIndex];
            if (verdicts == null) { return; }

            for (int col = 0; col < verdicts.Length; col++)
            {
                verdicts[col] = CellVerdict.Hidden;
            }

            uiState.VerdictsNeedRefreshing = true;
        }

        // Marks every cell across every row as Hidden. Called when starting a single-test run so
        // previously-resolved verdicts don't linger on inactive rows. Full-suite runs intentionally
        // don't call this — verdicts preserve between tests within the suite, accumulating as
        // each row resolves.
        public static void HideAllRowVerdicts(SimulateUIState uiState)
        {
            if (uiState.CellVerdicts == null) { return; }
            for (int row = 0; row < uiState.CellVerdicts.Length; row++)
            {
                CellVerdict[] verdicts = uiState.CellVerdicts[row];
                if (verdicts == null) { continue; }
                for (int col = 0; col < verdicts.Length; col++)
                {
                    verdicts[col] = CellVerdict.Hidden;
                }
            }

            uiState.VerdictsNeedRefreshing = true;
        }

        // Shows the overall results panel. Called when entering SuiteComplete from
        // SimulateModeSystem.ProcessResolvingTest (single-test or full-suite final row).
        //
        // HOOK: when the results panel work lands, wire it here.
        //   - add an inspector-assigned panel GameObject ref on SimulateUIState
        //     (e.g. ResultsPanelRoot).
        //   - SetActive(true), set header text via allCorrect.
        //   - uiState.ResultsPanelVisible = true.
        // Body intentionally empty until that work lands so the future implementer has a
        // single, named attachment point.
        public static void ShowResultsPanel(SimulateUIState uiState, bool allCorrect)
        {
            uiState.ResultsPanelVisible = true;
            ResultStateUtility.ShowResults(Find.State<ResultState>(), allCorrect);
        }

        // Hides the results panel. Called on Dismiss or on a fresh Play from SuiteComplete.
        public static void HideResultsPanel(SimulateUIState uiState)
        {
            // TODO: deactivate the results panel GameObject. Set ResultsPanelVisible = false.
            uiState.ResultsPanelVisible = false;
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
                uiState.Rows[row].Verdicts = new VerdictVisualizer[suite.Tests[row].Bundle.Length];
                uiState.Rows[row].RunButton.RowIndex = row;
                uiState.CellVerdicts[row] = new CellVerdict[suite.Tests[row].Bundle.Length];
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

                    // Cache the VerdictVisualizer ref for the refresh system. Output prefabs
                    // carry one; non-output prefabs don't, so the slot stays null.
                    uiState.Rows[row].Verdicts[col] = newCol.GetComponent<VerdictVisualizer>();
                }
            }
        }

        // Wires every content row's run button to HandleRunButtonClick. RowIndex was stamped
        // in CreateRowsAndCols, so the click handler reads the row from the button itself
        // rather than from the captured loop variable.
        private static void AssignRunListeners(SimulateUIState uiState, TestSuiteData suite, SimulateRunState runState)
        {
            for (int row = 0; row < suite.Tests.Length; row++)
            {
                SuiteRunRowButton btn = uiState.Rows[row].RunButton;
                btn.onClick.AddListener(() => HandleRunButtonClick(runState, uiState, btn.RowIndex));
            }
        }

        // Per-row click dispatch. Translates the player's intent (given current Phase /
        // CurrentRow) into the appropriate one-frame request flag.
        //
        //   active row + Propagating  -> Pause
        //   active row + Paused       -> Resume
        //   inactive row mid-run      -> Cancel current, queue this row to play after Cancelling lands in Idle
        //   otherwise (Idle / Done)   -> PlaySingleTest for this row
        private static void HandleRunButtonClick(SimulateRunState runState, SimulateUIState uiState, int rowIndex)
        {
            bool isActiveRow = (runState.CurrentRow == rowIndex);
            SimulatePhase phase = runState.Phase;

            if (isActiveRow && phase == SimulatePhase.Propagating)
            {
                SimulateControlUtility.RequestPause(runState);
            }
            else if (isActiveRow && phase == SimulatePhase.Paused)
            {
                SimulateControlUtility.RequestResume(runState);
            }
            else if (phase == SimulatePhase.Propagating || phase == SimulatePhase.Paused
                || phase == SimulatePhase.PreparingTest || phase == SimulatePhase.ResolvingTest)
            {
                // A different row is mid-run. Cancel it; PendingPlayRowIndex survives across the
                // Cancelling -> Idle transition and is consumed by ProcessIdle to re-fire the
                // queued PlaySingleTest.
                runState.PendingPlayRowIndex = rowIndex;
                SimulateControlUtility.RequestCancel(runState);
            }
            else
            {
                SimulateControlUtility.RequestPlaySingleTest(runState, rowIndex);
            }

            MarkAllRunButtonsDirty(uiState);
        }

        // Wires the suite-level run / restart / cancel buttons (classic mode) and the single
        // Test button (toggle mode) to their click handlers. Refs may be null until the prefab
        // layout for the suite-level toolbar is finalized; skip wiring any null slot rather than failing.
        private static void AssignSuiteListeners(SimulateUIState uiState, SimulateRunState runState)
        {
            if (uiState.SuiteRunButton != null)
            {
                uiState.SuiteRunButton.onClick.AddListener(() => HandleSuiteRunButtonClick(runState, uiState));
            }
            if (uiState.SuiteRestartButton != null)
            {
                uiState.SuiteRestartButton.onClick.AddListener(() => HandleSuiteRestartButtonClick(runState, uiState));
            }
            if (uiState.SuiteCancelButton != null)
            {
                uiState.SuiteCancelButton.onClick.AddListener(() => HandleSuiteCancelButtonClick(runState, uiState));
            }
            if (uiState.SuiteTestButton != null)
            {
                uiState.SuiteTestButton.onClick.AddListener(() => HandleSuiteTestButtonClick(runState, uiState));
            }
        }

        // Suite-level Play/Pause/Resume click. Mirrors HandleRunButtonClick but without an
        // active-row check — the suite button always controls the whole suite:
        //   Propagating  -> Pause
        //   Paused       -> Resume
        //   Idle / Done  -> PlayFullSuite
        // Other phases (PreparingTest, ResolvingTest, Cancelling) ignore the click implicitly:
        // RequestPlayFullSuite / Pause / Resume all guard via CanAccept* and no-op there.
        private static void HandleSuiteRunButtonClick(SimulateRunState runState, SimulateUIState uiState)
        {
            SimulatePhase phase = runState.Phase;
            if (phase == SimulatePhase.Propagating)
            {
                SimulateControlUtility.RequestPause(runState);
            }
            else if (phase == SimulatePhase.Paused)
            {
                SimulateControlUtility.RequestResume(runState);
            }
            else
            {
                SimulateControlUtility.RequestPlayFullSuite(runState);
            }

            MarkAllRunButtonsDirty(uiState);
        }

        // Suite-level Restart click. Always asks for a full-suite restart; CanAcceptRestartSuite
        // gates it (Propagating / Paused only).
        private static void HandleSuiteRestartButtonClick(SimulateRunState runState, SimulateUIState uiState)
        {
            SimulateControlUtility.RequestRestartSuite(runState);
            MarkAllRunButtonsDirty(uiState);
        }

        // Suite-level Cancel click. CanAcceptCancel gates it (any phase except Cancelling).
        // The button's interactable flag tightens this further to Propagating / Paused via
        // SuiteSecondaryButtonRefreshSystem.
        private static void HandleSuiteCancelButtonClick(SimulateRunState runState, SimulateUIState uiState)
        {
            SimulateControlUtility.RequestCancel(runState);
            MarkAllRunButtonsDirty(uiState);
        }

        // Toggle-input mode Test click. Reads the matched test-row index that
        // SuiteTestButtonRefreshSystem last computed; the request is dropped silently when no
        // row matches the current toggle combo (the button should also be greyed in that case).
        private static void HandleSuiteTestButtonClick(SimulateRunState runState, SimulateUIState uiState)
        {
            InputToggleState toggleState = Find.State<InputToggleState>();
            int matched = toggleState != null ? toggleState.LastMatchedRowIndex : -1;
            SimulateControlUtility.RequestPlayCurrentToggleCombo(runState, matched);
            MarkAllRunButtonsDirty(uiState);
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

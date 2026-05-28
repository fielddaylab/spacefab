using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using UnityEngine;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Holds the live state of the Simulate-mode evaluation machine: current phase, row/depth pointers,
    /// per-row verdicts, and the one-frame request flags driven by UI / Leaf. Replaces the ambient
    /// state that the prototype's VisualFeedbackRoutine kept in coroutine locals.
    /// </summary>
    public class SimulateRunState : SharedStateComponent, IRegistrationCallbacks
    {
        // ---- Phase machine ----

        [HideInInspector] public SimulatePhase Phase;
        [HideInInspector] public RunScope Scope;

        // Row currently executing or last executed. Valid once Phase advances past Idle.
        [HideInInspector] public int CurrentRow;

        // Payload for PlaySingleTestRequested: which row the UI asked to play.
        [HideInInspector] public int RequestedRowIndex;

        // Row queued to play once an in-flight Cancel finishes. -1 = nothing queued.
        // Set by the suite-row click handler when the player clicks an inactive row mid-run;
        // consumed by ProcessCancelling at the Cancelling -> Idle transition, which re-fires
        // PlaySingleTestRequested with this row index.
        [HideInInspector] public int PendingPlayRowIndex;

        // Depth pointer into SimulateGraphState.OrderedEdges.
        [HideInInspector] public int CurrentDepth;

        // Time spent at the current depth (or other timed sub-phase). Reset on depth advance / phase change.
        [HideInInspector] public float PhaseTimer;

        // True for exactly one frame when ProcessPropagating wants DepthStepSystem to paint this depth.
        // Cleared by SimulateControlRefreshSystem in LateUpdate.
        [HideInInspector] public bool PaintDepthThisFrame;

        // Set during Propagating if the current row produced an unstable flow anywhere.
        [HideInInspector] public bool IsUnstable;

        // ---- Inspector-editable pacing (matches prototype timeBetweenSteps / timeBetweenTests) ----

        public float InterDepthDelay = 0.5f;
        public float InterTestDelay = 2.0f;

        // ---- One-frame request flags (cleared by SimulateControlRefreshSystem in LateUpdate) ----

        [HideInInspector] public bool PlayFullSuiteRequested;
        [HideInInspector] public bool PlaySingleTestRequested;
        [HideInInspector] public bool PauseRequested;
        [HideInInspector] public bool ResumeRequested;
        [HideInInspector] public bool RestartTestRequested;
        [HideInInspector] public bool RestartSuiteRequested;
        [HideInInspector] public bool CancelRequested;
        [HideInInspector] public bool DismissResultsRequested; // TODO

        // Toggle-input mode: the player clicked Test, and InputToggleState.LastMatchedRowIndex
        // identified which TestData row to run. Carries through RequestedRowIndex like
        // PlaySingleTestRequested but skips the verdict-wipe so prior runs' verdicts persist.
        [HideInInspector] public bool PlayCurrentToggleComboRequested;

        // ---- Per-row verdicts. Sized to suite length on Simulate entry by ModeTransitionSystem. ----

        [HideInInspector] public TestRowVerdict[] RowVerdicts;

        public void OnRegister()
        {
            Phase = SimulatePhase.Idle;
            Scope = RunScope.FullSuite;
            CurrentRow = 0;
            RequestedRowIndex = 0;
            PendingPlayRowIndex = -1;
            CurrentDepth = 0;
            PhaseTimer = 0f;
            PaintDepthThisFrame = false;
            IsUnstable = false;

            PlayFullSuiteRequested = false;
            PlaySingleTestRequested = false;
            PauseRequested = false;
            ResumeRequested = false;
            RestartTestRequested = false;
            RestartSuiteRequested = false;
            CancelRequested = false;
            DismissResultsRequested = false;
            PlayCurrentToggleComboRequested = false;

            RowVerdicts = null;
        }

        public void OnDeregister()
        {
        }
    }

    /// <summary>
    /// Queries and commands for SimulateRunState. The Request* methods are the Leaf-integration surface
    /// (add [LeafMember] attributes when Leaf scripting is wired in). Commands only set one-frame flags —
    /// SimulateModeSystem owns the actual phase transitions.
    /// </summary>
    public static class SimulateControlUtility
    {
        #region Queries

        // True when the player can start a run (either scope). UI enables Play / play-one-row buttons.
        public static bool CanAcceptPlay(SimulateRunState runState)
        {
            return runState.Phase == SimulatePhase.Idle || runState.Phase == SimulatePhase.SuiteComplete;
        }

        // True when the player can pause (only mid-propagation).
        public static bool CanAcceptPause(SimulateRunState runState)
        {
            return runState.Phase == SimulatePhase.Propagating;
        }

        // True when the player can resume from Paused.
        public static bool CanAcceptResume(SimulateRunState runState)
        {
            return runState.Phase == SimulatePhase.Paused;
        }

        // True when restart-this-test is legal (Propagating or Paused).
        public static bool CanAcceptRestartTest(SimulateRunState runState)
        {
            return runState.Phase == SimulatePhase.Propagating || runState.Phase == SimulatePhase.Paused;
        }

        // True when restart-suite is legal (Propagating or Paused).
        public static bool CanAcceptRestartSuite(SimulateRunState runState)
        {
            return runState.Phase == SimulatePhase.Propagating || runState.Phase == SimulatePhase.Paused;
        }

        // True when Cancel is legal. Cancel is universal except inside Cancelling itself.
        public static bool CanAcceptCancel(SimulateRunState runState)
        {
            return runState.Phase != SimulatePhase.Cancelling;
        }

        // True when the results-panel dismiss button should be live (only in SuiteComplete).
        public static bool CanAcceptDismiss(SimulateRunState runState)
        {
            // TODO: return Phase == SuiteComplete.
            return false;
        }

        #endregion // Queries

        #region Commands

        // Request a run over the full test suite (row 0 → last).
        public static void RequestPlayFullSuite(SimulateRunState runState)
        {
            if (!CanAcceptPlay(runState)) { return; }
            runState.PlayFullSuiteRequested = true;
        }

        // Request a run of a single test row. rowIndex is carried on RequestedRowIndex.
        public static void RequestPlaySingleTest(SimulateRunState runState, int rowIndex)
        {
            if (!CanAcceptPlay(runState)) { return; }
            runState.RequestedRowIndex = rowIndex;
            runState.PlaySingleTestRequested = true;
        }

        // Toggle-input mode "Test" button click. matchedRowIndex is the TestData row whose Bundle
        // matches the player's current toggle combo (looked up via InputToggleUtility.FindMatchingTestRow).
        // Drops the request silently if the player isn't allowed to play right now or no row matched.
        public static void RequestPlayCurrentToggleCombo(SimulateRunState runState, int matchedRowIndex)
        {
            if (!CanAcceptPlay(runState)) { return; }
            if (matchedRowIndex < 0) { return; }
            runState.RequestedRowIndex = matchedRowIndex;
            runState.PlayCurrentToggleComboRequested = true;
        }

        public static void RequestPause(SimulateRunState runState)
        {
            if (!CanAcceptPause(runState)) { return; }
            runState.PauseRequested = true;
        }

        public static void RequestResume(SimulateRunState runState)
        {
            if (!CanAcceptResume(runState)) { return; }
            runState.ResumeRequested = true;
        }

        public static void RequestRestartTest(SimulateRunState runState)
        {
            if (!CanAcceptRestartTest(runState)) { return; }
            runState.RestartTestRequested = true;
        }

        public static void RequestRestartSuite(SimulateRunState runState)
        {
            if (!CanAcceptRestartSuite(runState)) { return; }
            runState.RestartSuiteRequested = true;
        }

        public static void RequestCancel(SimulateRunState runState)
        {
            if (!CanAcceptCancel(runState)) { return; }
            runState.CancelRequested = true;
        }

        public static void RequestDismissResults(SimulateRunState runState)
        {
            // TODO: if !CanAcceptDismiss, no-op. Otherwise set DismissResultsRequested = true.
        }

        #endregion // Commands

        #region Helpers

        // Resets every entry in RowVerdicts to Untested. Called by ProcessIdle when a new run starts.
        public static void ClearAllVerdicts(SimulateRunState runState)
        {
            if (runState.RowVerdicts == null) { return; }
            for (int i = 0; i < runState.RowVerdicts.Length; i++)
            {
                runState.RowVerdicts[i] = TestRowVerdict.Untested;
            }
        }

        // Classic per-run verdict wipe (model + UI). In toggle-input mode this is a no-op so
        // verdicts persist across Test clicks until the player edits the grid. Replaces every
        // inline ClearAllVerdicts + HideAllRowVerdicts pair in SimulateModeSystem so the gating
        // lives in exactly one place.
        public static void WipeVerdictsForNewRun(SimulateRunState runState, SimulateUIState uiState, DesignMinigameState designState)
        {
            if (designState != null && designState.UseToggleInputMode) { return; }
            ClearAllVerdicts(runState);
            SimulateUIUtility.HideAllRowVerdicts(uiState);
        }

        // Writes a verdict for a specific row; no-op on out-of-range index.
        public static void SetVerdict(SimulateRunState runState, int rowIndex, TestRowVerdict verdict)
        {
            if (runState.RowVerdicts == null) { return; }
            if (rowIndex < 0 || rowIndex >= runState.RowVerdicts.Length) { return; }
            runState.RowVerdicts[rowIndex] = verdict;
        }

        // Resets the active simulation back to a clean Idle state — wipes per-cell flow,
        // clears per-node transients, clears all row verdicts (model + UI), marks visuals dirty,
        // parks Phase at Idle, and flags the run-button icons for repaint. Shared by
        // SimulateModeSystem.ProcessCancelling and ModeTransitionSystem.ExitSimulateMode.
        // Intentionally does NOT touch PendingPlayRowIndex so callers can decide whether to
        // consume or discard a queued play.
        //
        // In toggle-input mode the verdict wipe is suppressed: verdicts persist until the player
        // edits the grid (handled by DesignTriggers.HandleGridModified). Exiting Simulate to pick
        // a tool, or hitting Cancel mid-prop, leaves prior verdicts on the table.
        public static void WipeRunState(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState, VisualGridStackState visualState, DesignMinigameState designState)
        {
            SimulateRunScratchUtility.BumpFlowStamp(runScratch);
            SimulateRunScratchUtility.ClearNodeTransients(runScratch, graphState.NodeCount);
            visualState.VisualsNeedRefreshing = true;

            if (designState == null || !designState.UseToggleInputMode)
            {
                ClearAllVerdicts(runState);
                SimulateUIUtility.HideAllRowVerdicts(uiState);
            }

            runState.Phase = SimulatePhase.Idle;
            SimulateUIUtility.MarkAllRunButtonsDirty(uiState);
        }

        #endregion // Helpers
    }
}

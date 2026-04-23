using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design
{
    /// <summary>
    /// Phase driver for Simulate mode. Owns the SimulatePhase state machine: reads one-frame request
    /// flags on SimulateRunState, decides whether this frame advances a depth step or transitions to a
    /// new phase, and sets PaintDepthThisFrame on the frames DepthStepSystem should do flow work.
    /// Runs on Update at order 1 under SimulateModeMask.
    /// </summary>
    public class SimulateModeSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 1, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateRunState>()
                    .ReadWriteShared<SimulateGraphState>()
                    .ReadShared<GridStackState>()
                    .ReadWriteShared<SimulateUIState>()
            );
        }

        // Top-level phase dispatch. Cancel is checked first and wins over any other request.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out SimulateRunState runState,
                out SimulateGraphState graphState,
                out GridStackState gridStackState
                );
            SimulateUIState uiState = Find.State<SimulateUIState>();

            // Universal high-priority request: Cancel beats everything except Cancelling itself.
            if (runState.Phase != SimulatePhase.Cancelling && runState.CancelRequested)
            {
                EnterCancelling(runState);
                return;
            }

            switch (runState.Phase)
            {
                case SimulatePhase.Idle:
                    ProcessIdle(runState);
                    break;
                case SimulatePhase.PreparingTest:
                    ProcessPreparingTest(runState, gridStackState, uiState);
                    break;
                case SimulatePhase.Propagating:
                    ProcessPropagating(runState, graphState, uiState, deltaTime);
                    break;
                case SimulatePhase.Paused:
                    ProcessPaused(runState);
                    break;
                case SimulatePhase.ResolvingTest:
                    ProcessResolvingTest(runState, graphState, uiState);
                    break;
                case SimulatePhase.SuiteComplete:
                    ProcessSuiteComplete(runState, uiState);
                    break;
                case SimulatePhase.Cancelling:
                    ProcessCancelling(runState, graphState, gridStackState, uiState);
                    break;
            }
        }

        // Idle: accept Play / PlaySingle. Graph is already built by ModeTransitionSystem on
        // Simulate-mode entry, so we can go straight to PreparingTest without a build phase.
        static private void ProcessIdle(SimulateRunState runState)
        {
            // TODO: if PlayFullSuiteRequested:
            //           Scope = FullSuite; CurrentRow = 0;
            //           SimulateControlUtility.ClearAllVerdicts(runState);
            //           Phase = PreparingTest;
            //           dispatch DesignSimPlayStarted.
            // TODO: if PlaySingleTestRequested:
            //           Scope = SingleTest; CurrentRow = RequestedRowIndex;
            //           SimulateControlUtility.ClearAllVerdicts(runState);   // clear ALL rows to Untested
            //           Phase = PreparingTest;
            //           dispatch DesignSimPlayStarted.
        }

        // PreparingTest: reset per-row sim state on grid (flow, temp inversions), push inputs to UI.
        // Advances to Propagating immediately.
        static private void ProcessPreparingTest(SimulateRunState runState, GridStackState gridStackState, SimulateUIState uiState)
        {
            // TODO: reset FlowState/TempTransformation on all grid cells (per-row wipe).
            // TODO: zero CurrFlowState/TempTransformedType on CrucialNodes (once graph-state fields exist).
            // TODO: SimulateUIUtility.WriteRowInputs(uiState, CurrentRow, suite.Tests[CurrentRow]).
            // TODO: runState.IsUnstable = false.
            // TODO: runState.CurrentDepth = 0; runState.PhaseTimer = 0; Phase = Propagating.
            // TODO: dispatch DesignSimRowStarted with CurrentRow payload.
        }

        // Propagating: per-depth paint rhythm.
        //   Entry-to-new-depth frame → PaintDepthThisFrame = true; PhaseTimer = 0.
        //   Subsequent frames at same depth → accumulate PhaseTimer; PaintDepthThisFrame stays false.
        //   At PhaseTimer >= InterDepthDelay → depth boundary; check interrupts, then advance or resolve.
        static private void ProcessPropagating(SimulateRunState runState, SimulateGraphState graphState, SimulateUIState uiState, float deltaTime)
        {
            // TODO: if PhaseTimer == 0 on first frame of this depth, PaintDepthThisFrame = true; return.
            //       (Also covers CurrentDepth==0 entry from PreparingTest.)
            // TODO: PhaseTimer += deltaTime. If PhaseTimer < InterDepthDelay, return (visuals playing).
            //
            // Depth boundary reached — check interrupts:
            // TODO: if RestartTestRequested → Phase = PreparingTest (CurrentRow unchanged); return.
            // TODO: if RestartSuiteRequested → Scope = FullSuite; CurrentRow = 0;
            //         SimulateControlUtility.ClearAllVerdicts; Phase = PreparingTest; return.
            // TODO: if PauseRequested → Phase = Paused; dispatch DesignSimPaused; return.
            //
            // No interrupt — advance depth or finish:
            // TODO: CurrentDepth++. If CurrentDepth > graphState.MaxDepth → Phase = ResolvingTest;
            //       PhaseTimer = 0; return.
            // TODO: Otherwise PhaseTimer = 0; (next frame will paint.)
        }

        // Paused: wait for Resume or Restart*. Cancel handled at top of ProcessWork.
        static private void ProcessPaused(SimulateRunState runState)
        {
            // TODO: if ResumeRequested → Phase = Propagating; dispatch DesignSimResumed; return.
            // TODO: if RestartTestRequested → Phase = PreparingTest (CurrentRow unchanged); return.
            // TODO: if RestartSuiteRequested → Scope = FullSuite; CurrentRow = 0;
            //         SimulateControlUtility.ClearAllVerdicts; Phase = PreparingTest; return.
        }

        // ResolvingTest: score outputs for CurrentRow, write verdict, advance to next row or finish.
        static private void ProcessResolvingTest(SimulateRunState runState, SimulateGraphState graphState, SimulateUIState uiState)
        {
            // TODO: compute allCorrect by comparing CrucialNode output flows to expected suite values.
            //       verdict = IsUnstable ? Unstable : (allCorrect ? Correct : Incorrect).
            // TODO: SimulateControlUtility.SetVerdict(runState, CurrentRow, verdict).
            // TODO: SimulateUIUtility.WriteRowVerdict(uiState, CurrentRow, verdict, outputFlows).
            // TODO: dispatch DesignSimRowResolved with (CurrentRow, verdict).
            //
            // Advance based on Scope:
            // TODO: if Scope == SingleTest → Phase = SuiteComplete; SimulateUIUtility.ShowResultsPanel(...).
            //       dispatch DesignSimSuiteComplete.
            // TODO: if Scope == FullSuite:
            //         if CurrentRow + 1 < RowVerdicts.Length → CurrentRow++; Phase = PreparingTest.
            //         else → Phase = SuiteComplete; SimulateUIUtility.ShowResultsPanel(...); dispatch DesignSimSuiteComplete.
        }

        // SuiteComplete: wait on Dismiss or new Play request. Cancel handled at top.
        static private void ProcessSuiteComplete(SimulateRunState runState, SimulateUIState uiState)
        {
            // TODO: if DismissResultsRequested → SimulateUIUtility.HideResultsPanel(uiState); Phase = Idle.
            // TODO: if PlayFullSuiteRequested → Scope = FullSuite; CurrentRow = 0;
            //         SimulateControlUtility.ClearAllVerdicts; SimulateUIUtility.HideResultsPanel;
            //         Phase = PreparingTest.
            // TODO: if PlaySingleTestRequested → Scope = SingleTest; CurrentRow = RequestedRowIndex;
            //         SimulateControlUtility.ClearAllVerdicts; SimulateUIUtility.HideResultsPanel;
            //         Phase = PreparingTest.
        }

        // Cancelling: wipe sim visuals and exit Simulate mode via ModeTransitionState.
        static private void ProcessCancelling(SimulateRunState runState, SimulateGraphState graphState, GridStackState gridStackState, SimulateUIState uiState)
        {
            // TODO: reset FlowState/TempTransformation on every grid cell.
            // TODO: SimulateUIUtility.ClearAllEvalMarks(uiState); HideResultsPanel(uiState).
            // TODO: dispatch DesignSimCancelled.
            // TODO: request Simulate→Tool transition via ModeTransitionState (or directly:
            //         GameLoop.SuspendUpdates(UpdateMasks.SimulateModeMask);
            //         SimulateGraphUtility.Clear(graphState);
            //         GameLoop.ResumeUpdates(UpdateMasks.ToolModeMask)).
            //       Final transition wiring lives in ModeTransitionSystem — this handler just signals.
            // TODO: Phase = Idle (so a subsequent Simulate entry starts clean).
        }

        // Sets Phase = Cancelling and clears per-test progress. Does not itself perform the wipe —
        // that's ProcessCancelling's job on the next tick.
        static private void EnterCancelling(SimulateRunState runState)
        {
            // TODO: Phase = Cancelling; PhaseTimer = 0; CurrentDepth = 0; PaintDepthThisFrame = false.
        }
    }
}

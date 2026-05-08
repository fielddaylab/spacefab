using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;

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
                    .ReadWriteShared<SimulateRunScratch>()
                    .ReadShared<GridStackState>()
                    .ReadWriteShared<SimulateUIState>()
                    .ReadWriteShared<VisualGridStackState>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        // Top-level phase dispatch. Cancel is checked first and wins over any other request.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out SimulateRunState runState,
                out SimulateGraphState graphState,
                out SimulateRunScratch runScratch,
                out GridStackState gridStackState
                );
            Find.State(
                out SimulateUIState uiState,
                out VisualGridStackState visualState,
                out PlayerProgressState progressState
                );

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
                    ProcessPreparingTest(runState, runScratch, graphState, uiState, visualState, progressState, gridStackState);
                    break;
                case SimulatePhase.Propagating:
                    ProcessPropagating(runState, graphState, uiState, deltaTime);
                    break;
                case SimulatePhase.Paused:
                    ProcessPaused(runState);
                    break;
                case SimulatePhase.ResolvingTest:
                    ProcessResolvingTest(runState, runScratch, graphState, uiState, progressState, gridStackState);
                    break;
                case SimulatePhase.SuiteComplete:
                    ProcessSuiteComplete(runState, uiState);
                    break;
                case SimulatePhase.Cancelling:
                    ProcessCancelling(runState, runScratch, graphState, uiState, visualState);
                    break;
            }
        }

        // Idle: accept Play / PlaySingle. Graph is already built by ModeTransitionSystem on
        // Simulate-mode entry, so we can go straight to PreparingTest without a build phase.
        // Branch order: a queued cancel-then-play (PendingPlayRowIndex) wins over an explicit
        // PlayFullSuiteRequested if both somehow set the same frame, since the queue carries an
        // older intent. PlayFullSuiteRequested then wins over PlaySingleTestRequested.
        static private void ProcessIdle(SimulateRunState runState)
        {
            // Cancel-then-play hand-off. SuiteRunRowButton's click handler sets PendingPlayRowIndex
            // when the player clicks an inactive row mid-run; ProcessCancelling preserved it across
            // the Cancelling -> Idle transition for us to consume here.
            if (runState.PendingPlayRowIndex >= 0)
            {
                runState.Scope = RunScope.SingleTest;
                runState.CurrentRow = runState.PendingPlayRowIndex;
                runState.PendingPlayRowIndex = -1;
                SimulateControlUtility.ClearAllVerdicts(runState);
                runState.Phase = SimulatePhase.PreparingTest;
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimPlayStarted);
                return;
            }

            if (runState.PlayFullSuiteRequested)
            {
                runState.Scope = RunScope.FullSuite;
                runState.CurrentRow = 0;
                SimulateControlUtility.ClearAllVerdicts(runState);
                runState.Phase = SimulatePhase.PreparingTest;
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimPlayStarted);
                return;
            }

            if (runState.PlaySingleTestRequested)
            {
                runState.Scope = RunScope.SingleTest;
                runState.CurrentRow = runState.RequestedRowIndex;
                SimulateControlUtility.ClearAllVerdicts(runState);
                runState.Phase = SimulatePhase.PreparingTest;
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimPlayStarted);
                return;
            }
        }

        // PreparingTest: reset per-row sim state, prime input flows, advance to Propagating.
        // Runs exactly once per test-row start.
        //
        // Reset strategy:
        //   - Per-node transient arrays (NodeFlow / NodeTempTransform): Array.Clear on 0..NodeCount.
        //     NodeCount is tens at most — effectively free.
        //   - Per-cell transient state (CellFlow / CellTempTransform): BumpFlowStamp. Single int
        //     increment invalidates every per-cell mark from the prior test.
        //   - Edge state: none to reset. Cycle-detection flags are durable on CrucialEdge and
        //     computed at Build time, not per test.
        static private void ProcessPreparingTest(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState, VisualGridStackState visualState, PlayerProgressState progressState, GridStackState gridStackState)
        {
            // Per-node transient reset. Cheap: NodeCount is small.
            SimulateRunScratchUtility.ClearNodeTransients(runScratch, graphState.NodeCount);

            // Per-cell transient reset. O(1) — stamp bump invalidates all prior flow + temp-
            // transform writes without touching the arrays themselves.
            SimulateRunScratchUtility.BumpFlowStamp(runScratch);

            // Prime InputFlowByNode for this row. Walk Input crucial nodes and materialize their
            // test-row value once; DepthStepSystem reads from the array per edge, avoiding a
            // per-edge TestData scan.
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(progressState.ContractAssetsWrapperId);
            TestSuiteData suite = contractAssets.DesignLevelData.GetTestSuite();
            TestData currTest = suite.Tests[runState.CurrentRow];
            for (int i = 0; i < graphState.NodeCount; i++)
            {
                CrucialNode node = graphState.CrucialNodes[i];
                GridCell cell = GridStackUtility.GetCellDirect(gridStackState, node.Coord);
                if (cell.CellType == CellType.Input)
                {
                    runScratch.InputFlowByNode[i] = EvalUtility.GetTestValBySubType(cell.SubtypeLabel, currTest);
                }
            }
            SimulateUIUtility.WriteRowInputs(uiState, runState.CurrentRow, currTest);

            // Mark visuals dirty so GridVisualsUpdateSystem redraws the now-empty-flow grid.
            visualState.VisualsNeedRefreshing = true;

            runState.IsUnstable = false;
            runScratch.IsUnstable = false;
            uiState.HighlightedRowIndex = runState.CurrentRow;

            runState.CurrentDepth = 0;
            runState.PhaseTimer = 0f;
            runState.Phase = SimulatePhase.Propagating;

            // CurrentRow / Phase just changed; the active row's button needs to flip to Pause.
            uiState.RunButtonsNeedRefreshing = true;

            SpacefabGame.Events.Dispatch(GameEvents.DesignSimRowStarted, runState.CurrentRow);
        }

        // Propagating: per-depth paint rhythm.
        //   First frame at a new depth (PhaseTimer == 0) → set PaintDepthThisFrame so DepthStepSystem
        //     paints this depth's edges this same frame (it runs at Update order 2, after us at 1).
        //   Every frame (including the paint frame) → accumulate deltaTime. The first frame's
        //     accumulation is what pulls PhaseTimer off zero so the next frame doesn't re-paint.
        //   At PhaseTimer >= InterDepthDelay → depth boundary; check interrupts, then advance or resolve.
        static private void ProcessPropagating(SimulateRunState runState, SimulateGraphState graphState, SimulateUIState uiState, float deltaTime)
        {
            // First frame at this depth: signal DepthStepSystem to paint.
            if (runState.PhaseTimer == 0f)
            {
                runState.PaintDepthThisFrame = true;
            }

            // Accumulate. Even on the paint frame: ensures next frame won't repaint and progress
            // is made toward the InterDepthDelay threshold.
            runState.PhaseTimer += deltaTime;
            if (runState.PhaseTimer < runState.InterDepthDelay) { return; }

            // Depth boundary reached. Check interrupts before advancing.
            // TODO: if RestartTestRequested → Phase = PreparingTest (CurrentRow unchanged); return.
            // TODO: if RestartSuiteRequested → Scope = FullSuite; CurrentRow = 0;
            //         SimulateControlUtility.ClearAllVerdicts; Phase = PreparingTest; return.
            // TODO: if PauseRequested → Phase = Paused; dispatch DesignSimPaused; return.
            //       Pause/Resume/Restart are deferred — SuiteRunRowButton can request them but the
            //       phase machine ignores those flags until this branch is implemented.

            // No interrupt: advance depth or finish.
            runState.CurrentDepth++;
            runState.PhaseTimer = 0f;
            if (runState.CurrentDepth > graphState.MaxDepth)
            {
                runState.Phase = SimulatePhase.ResolvingTest;
                // Active row's button should flip back from Pause to Play.
                uiState.RunButtonsNeedRefreshing = true;
            }
            // Otherwise PhaseTimer == 0 → next frame's first-frame branch paints the new depth.
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
        //
        // Reads per-output flow from runScratch.NodeFlow, fills runScratch.OutputFlowBuffer in
        // CrucialNodes-Output order, and compares each value to the expected one from the current
        // row's TestData. The verdict gets recorded on runState.RowVerdicts via SetVerdict and
        // pushed to the UI via WriteRowVerdict (currently a stub — UI not yet implemented).
        //
        // Advance rules:
        //   SingleTest scope                       → SuiteComplete (whole run is just this row).
        //   FullSuite, more rows remain            → next row via PreparingTest.
        //   FullSuite, last row resolved           → SuiteComplete with aggregate-correct flag.
        static private void ProcessResolvingTest(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState, PlayerProgressState progressState, GridStackState gridStackState)
        {
            ContractAssetsWrapper contractAssets = Find.NamedAsset<ContractAssetsWrapper>(progressState.ContractAssetsWrapperId);
            TestSuiteData suite = contractAssets.DesignLevelData.GetTestSuite();
            TestData currTest = suite.Tests[runState.CurrentRow];

            // Score every Output crucial node against its expected value. OutputFlowBuffer is
            // sized by SimulateRunScratchUtility.SizeOutputBuffer at Simulate-mode entry, in the
            // same CrucialNodes ordering — so outputIdx walks both in lockstep.
            bool allCorrect = true;
            int outputIdx = 0;
            for (int i = 0; i < graphState.NodeCount; i++)
            {
                CrucialNode node = graphState.CrucialNodes[i];
                GridCell cell = GridStackUtility.GetCellDirect(gridStackState, node.Coord);
                if (cell.CellType != CellType.Output) { continue; }

                FlowState actual = runScratch.NodeFlow[i];
                FlowState expected = EvalUtility.GetTestValBySubType(cell.SubtypeLabel, currTest);
                runScratch.OutputFlowBuffer[outputIdx++] = actual;
                if (actual != expected) { allCorrect = false; }
            }

            // Unstable beats Correct/Incorrect: any unstable flow this row, even if all outputs
            // happened to match expectations, is a fail-by-instability per the prototype.
            TestRowVerdict verdict = runState.IsUnstable
                ? TestRowVerdict.Unstable
                : (allCorrect ? TestRowVerdict.Correct : TestRowVerdict.Incorrect);
            SimulateControlUtility.SetVerdict(runState, runState.CurrentRow, verdict);
            SimulateUIUtility.WriteRowVerdict(uiState, runState.CurrentRow, verdict, runScratch.OutputFlowBuffer);
            SpacefabGame.Events.Dispatch(GameEvents.DesignSimRowResolved, runState.CurrentRow);

            // Advance based on Scope.
            if (runState.Scope == RunScope.SingleTest)
            {
                runState.Phase = SimulatePhase.SuiteComplete;
                SimulateUIUtility.ShowResultsPanel(uiState, verdict == TestRowVerdict.Correct);
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimSuiteComplete);
            }
            else if (runState.CurrentRow + 1 < runState.RowVerdicts.Length)
            {
                runState.CurrentRow++;
                runState.Phase = SimulatePhase.PreparingTest;
            }
            else
            {
                runState.Phase = SimulatePhase.SuiteComplete;
                SimulateUIUtility.ShowResultsPanel(uiState, IsAllCorrect(runState.RowVerdicts));
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimSuiteComplete);
            }

            // Phase or CurrentRow changed; the active row's button needs a repaint.
            uiState.RunButtonsNeedRefreshing = true;
        }

        // True iff every entry in verdicts is Correct. Used for the suite-level pass/fail flag
        // pushed to the results panel at the end of a FullSuite run.
        static private bool IsAllCorrect(TestRowVerdict[] verdicts)
        {
            for (int i = 0; i < verdicts.Length; i++)
            {
                if (verdicts[i] != TestRowVerdict.Correct) { return false; }
            }
            return true;
        }

        // SuiteComplete: wait on Dismiss or new Play request. Cancel handled at top.
        // Both Play branches wipe all row verdicts and hide the results panel before re-running.
        static private void ProcessSuiteComplete(SimulateRunState runState, SimulateUIState uiState)
        {
            if (runState.DismissResultsRequested)
            {
                SimulateUIUtility.HideResultsPanel(uiState);
                runState.Phase = SimulatePhase.Idle;
                uiState.RunButtonsNeedRefreshing = true;
                return;
            }

            if (runState.PlayFullSuiteRequested)
            {
                runState.Scope = RunScope.FullSuite;
                runState.CurrentRow = 0;
                SimulateControlUtility.ClearAllVerdicts(runState);
                SimulateUIUtility.HideResultsPanel(uiState);
                runState.Phase = SimulatePhase.PreparingTest;
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimPlayStarted);
                return;
            }

            if (runState.PlaySingleTestRequested)
            {
                runState.Scope = RunScope.SingleTest;
                runState.CurrentRow = runState.RequestedRowIndex;
                SimulateControlUtility.ClearAllVerdicts(runState);
                SimulateUIUtility.HideResultsPanel(uiState);
                runState.Phase = SimulatePhase.PreparingTest;
                SpacefabGame.Events.Dispatch(GameEvents.DesignSimPlayStarted);
                return;
            }
        }

        // Cancelling: wipe sim visuals and exit Simulate mode via ModeTransitionState.
        //
        // Flow-wipe is O(1): bump CurrentFlowStamp, which invalidates every per-cell flow and
        // temp-transform mark. GridVisualsUpdateSystem then redraws with empty flow.
        static private void ProcessCancelling(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState, VisualGridStackState visualState)
        {
            // Shared sim-state wipe. Lands runState.Phase at Idle so a subsequent Simulate
            // entry starts clean. PendingPlayRowIndex is intentionally NOT touched here —
            // ProcessIdle consumes it on the next frame to fire the queued PlaySingleTest
            // (cancel-then-play hand-off from the suite-row click handler).
            SimulateControlUtility.WipeRunState(runState, runScratch, graphState, uiState, visualState);

            // TODO: SimulateUIUtility.ClearAllEvalMarks(uiState); HideResultsPanel(uiState).
            // TODO: dispatch DesignSimCancelled.
            // TODO: request Simulate→Tool transition via ModeTransitionState (or directly:
            //         GameLoop.SuspendUpdates(UpdateMasks.SimulateModeMask);
            //         SimulateGraphUtility.Clear(graphState);
            //         GameLoop.ResumeUpdates(UpdateMasks.ToolModeMask)).
            //       Final transition wiring lives in ModeTransitionSystem — this handler just signals.
        }

        // Sets Phase = Cancelling and clears per-test progress. Does not itself perform the wipe —
        // that's ProcessCancelling's job on the next tick.
        static private void EnterCancelling(SimulateRunState runState)
        {
            // TODO: Phase = Cancelling; PhaseTimer = 0; CurrentDepth = 0; PaintDepthThisFrame = false.
        }
    }
}

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
                out VisualGridStackState visualState
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
                    ProcessPreparingTest(runState, runScratch, graphState, uiState, visualState);
                    break;
                case SimulatePhase.Propagating:
                    ProcessPropagating(runState, graphState, uiState, deltaTime);
                    break;
                case SimulatePhase.Paused:
                    ProcessPaused(runState);
                    break;
                case SimulatePhase.ResolvingTest:
                    ProcessResolvingTest(runState, runScratch, graphState, uiState);
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
        static private void ProcessPreparingTest(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState, VisualGridStackState visualState)
        {
            // Per-node transient reset. Cheap: NodeCount is small.
            SimulateRunScratchUtility.ClearNodeTransients(runScratch, graphState.NodeCount);

            // Per-cell transient reset. O(1) — stamp bump invalidates all prior flow + temp-
            // transform writes without touching the arrays themselves.
            SimulateRunScratchUtility.BumpFlowStamp(runScratch);

            // Prime InputFlowByNode for this row. Walk Input crucial nodes and materialize their
            // test-row value once; DepthStepSystem reads from the array per edge, avoiding a
            // per-edge TestData scan.
            //
            // MISSING DEPENDENCY: TestSuiteData for the current level is not yet plumbed through
            // to Design's runtime state. When that pipeline lands (LevelData threaded through
            // ContractAssetsWrapper → DesignMinigameState → here), uncomment the block below
            // and delete this TODO. For now, InputFlowByNode stays zeroed (FlowState.Empty)
            // which means Input-origin edges will propagate empty flow — harmless in the
            // scaffold, but Simulate mode will not produce correct results until wired.
            //
            // TODO(level-data): replace with:
            //   var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(progressState.ContractAssetsWrapperId);
            //   TestSuiteData suite = contractAssets.DesignLevelData.GetTestSuite();
            //   TestData currTest = suite.Tests[runState.CurrentRow];
            //   for (int i = 0; i < graphState.NodeCount; i++) {
            //       CrucialNode node = graphState.CrucialNodes[i];
            //       GridCell cell = GridStackUtility.GetCellDirect(gridStackState, node.Coord);
            //       if (cell.CellType == CellType.Input) {
            //           runScratch.InputFlowByNode[i] = EvalUtility.GetTestValBySubType(cell.SubtypeLabel, currTest);
            //       }
            //   }
            //   SimulateUIUtility.WriteRowInputs(uiState, runState.CurrentRow, currTest);

            // Mark visuals dirty so GridVisualsUpdateSystem redraws the now-empty-flow grid.
            visualState.VisualsNeedRefreshing = true;

            runState.IsUnstable = false;
            runScratch.IsUnstable = false;
            uiState.HighlightedRowIndex = runState.CurrentRow;

            runState.CurrentDepth = 0;
            runState.PhaseTimer = 0f;
            runState.Phase = SimulatePhase.Propagating;

            SpacefabGame.Events.Dispatch(GameEvents.DesignSimRowStarted, runState.CurrentRow);
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
        //
        // Per the flow-propagation plan, this handler reads per-output flow from runScratch.NodeFlow
        // into runScratch.OutputFlowBuffer and compares each to expected values from the current
        // test row. Only the OutputFlowBuffer collection is in scope for the propagation port;
        // verdict decision, row advancement, and suite-completion wiring remain scaffolded until
        // the phase machine's control-flow handlers are implemented.
        static private void ProcessResolvingTest(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState)
        {
            // MISSING DEPENDENCY: same level-data gap as ProcessPreparingTest. When the current
            // level's TestSuiteData is reachable here, scoring can be wired. For now, collect
            // OutputFlowBuffer from runScratch.NodeFlow so downstream consumers have somewhere
            // to read from.
            //
            // TODO(level-data): replace with full scoring:
            //   var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(progressState.ContractAssetsWrapperId);
            //   TestSuiteData suite = contractAssets.DesignLevelData.GetTestSuite();
            //   TestData currTest = suite.Tests[runState.CurrentRow];
            //   bool allCorrect = true;
            //   int outputIdx = 0;
            //   for (int i = 0; i < graphState.NodeCount; i++) {
            //       CrucialNode node = graphState.CrucialNodes[i];
            //       GridCell cell = GridStackUtility.GetCellDirect(gridStackState, node.Coord);
            //       if (cell.CellType != CellType.Output) { continue; }
            //       FlowState actual = runScratch.NodeFlow[i];
            //       FlowState expected = EvalUtility.GetTestValBySubType(cell.SubtypeLabel, currTest);
            //       runScratch.OutputFlowBuffer[outputIdx++] = actual;
            //       if (actual != expected) { allCorrect = false; }
            //   }
            //   TestRowVerdict verdict = runState.IsUnstable
            //       ? TestRowVerdict.Unstable
            //       : (allCorrect ? TestRowVerdict.Correct : TestRowVerdict.Incorrect);
            //   SimulateControlUtility.SetVerdict(runState, runState.CurrentRow, verdict);
            //   SimulateUIUtility.WriteRowVerdict(uiState, runState.CurrentRow, verdict, runScratch.OutputFlowBuffer);
            //   Game.Events.Dispatch(GameEvents.DesignSimRowResolved, runState.CurrentRow);
            //
            //   Advance based on Scope:
            //   if (runState.Scope == RunScope.SingleTest) {
            //       runState.Phase = SimulatePhase.SuiteComplete;
            //       SimulateUIUtility.ShowResultsPanel(uiState, verdict == TestRowVerdict.Correct);
            //       Game.Events.Dispatch(GameEvents.DesignSimSuiteComplete);
            //   } else if (runState.CurrentRow + 1 < runState.RowVerdicts.Length) {
            //       runState.CurrentRow++;
            //       runState.Phase = SimulatePhase.PreparingTest;
            //   } else {
            //       runState.Phase = SimulatePhase.SuiteComplete;
            //       SimulateUIUtility.ShowResultsPanel(uiState, /* aggregate of RowVerdicts */);
            //       Game.Events.Dispatch(GameEvents.DesignSimSuiteComplete);
            //   }
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
        //
        // Flow-wipe is O(1): bump CurrentFlowStamp, which invalidates every per-cell flow and
        // temp-transform mark. GridVisualsUpdateSystem then redraws with empty flow.
        static private void ProcessCancelling(SimulateRunState runState, SimulateRunScratch runScratch, SimulateGraphState graphState, SimulateUIState uiState, VisualGridStackState visualState)
        {
            SimulateRunScratchUtility.BumpFlowStamp(runScratch);
            SimulateRunScratchUtility.ClearNodeTransients(runScratch, graphState.NodeCount);
            visualState.VisualsNeedRefreshing = true;

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

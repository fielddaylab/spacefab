using BeauUtil;
using FieldDay;
using FieldDay.Music;
using FieldDay.Scripting;
using FieldDay.Systems;
using FieldDay.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Top-level orchestrator for entering the overarching scene from a cold start or after
    /// a minigame: loads the current chapter, optionally plays the contract-completion sequence,
    /// loads available contracts, runs select/confirm/load for the active contract, then resumes
    /// OverarchingMask. Runs on Update at order 0 under SetupMask. Sequence:
    ///   1. Load the present chapter
    ///   2. Load current available contracts (parallel with completion sequence)
    ///   2b. If coming from previous chapter: run completion sequence
    ///   3. If no contract selected: run select sequence
    ///   4. Load selected contract
    /// </summary>
    public class OverarchingStartupSequenceSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadWriteShared<OverarchingStartupSequenceState>()
                    .ReadWriteShared<ChapterLoadState>()
                    .ReadWriteShared<ContractCompletionState>()
                    .ReadWriteShared<ContractSelectState>()
                    .ReadShared<ChapterState>()
                    .ReadWriteShared<ContractLoadState>()
                    .ReadWriteShared<ContractConfirmState>()
                    .ReadShared<SharedUIState>()
                    .ReadWriteShared<PlayerProgressState>()
                    .ReadWriteShared<ProgressMeterState>()
            );
        }

        // Dispatches to the handler for the current startup phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out OverarchingStartupSequenceState startupState,
                out ChapterLoadState chapterLoadState,
                out ContractCompletionState completionState,
                out ContractSelectState selectState
                );
            Find.State(
                out ChapterState chapterState,
                out ContractLoadState contractLoadState,
                out ContractConfirmState confirmState,
                out SharedUIState uiState
                );
            Find.State(
                out PlayerProgressState progressState,
                out ProgressMeterState meterState
                );

            // Apply initial wiki unlocks if first time ever entering the scene
            PlayerProgressUtility.TryApplyInitialWikiUnlocks(progressState);

            // Gate: run only if we haven't finished startup and the UI isn't mid-load
            if (!(startupState.Phase != OverarchingStartupSequencePhase.Completed && !uiState.IsLoading)) {
                return;
            }

            switch (startupState.Phase) {
                case OverarchingStartupSequencePhase.LoadCurrChapter:
                    ProcessLoadCurrChapter(startupState, chapterLoadState, completionState, progressState);
                    break;
                case OverarchingStartupSequencePhase.ContractCompletionSystem:
                    ProcessContractCompletion(startupState, completionState);
                    break;
                case OverarchingStartupSequencePhase.LoadCurrAvailableContracts:
                    ProcessLoadCurrAvailableContracts(startupState, chapterLoadState, selectState, chapterState);
                    break;
                case OverarchingStartupSequencePhase.ContractSelectSystem:
                    ProcessContractSelectSystem(startupState, chapterLoadState, selectState, confirmState);
                    break;
                case OverarchingStartupSequencePhase.ContractConfirmSystem:
                    var prevConfirmPhase = confirmState.Phase;
                    ProcessContractConfirmSystem(startupState, contractLoadState, confirmState);
                    break;
                case OverarchingStartupSequencePhase.LoadSelectedContract:
                    ProcessLoadSelectedContract(startupState, chapterLoadState, contractLoadState, meterState);
                    break;
                default:
                    break;
            }
        }

        // Kicks off ChapterLoadSystem. When it completes, branches to contract-completion or straight to loading contracts.
        static private void ProcessLoadCurrChapter(OverarchingStartupSequenceState startupState, ChapterLoadState chapterLoadState, ContractCompletionState completionState, PlayerProgressState progressState) {
            if (chapterLoadState.Phase == ChapterLoadPhase.Waiting) {
                // begin ChapterLoadSystem
                GameLoop.ResumeUpdates(UpdateMasks.ChapterMask);
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ChapterLoadSystem");
                chapterLoadState.Phase = ChapterLoadPhase.LoadingChapter;
            }
            else {
                if (chapterLoadState.Phase == ChapterLoadPhase.Completed) {
                    // Decide whether to run the contract-completion sequence
                    if (progressState.RecentlyCompletedChapter) {
                        startupState.Phase = OverarchingStartupSequencePhase.ContractCompletionSystem;
                        completionState.Phase = ContractCompletionPhase.Waiting;
                        progressState.RecentlyCompletedChapter = false;
                    }
                    else {
                        startupState.Phase = OverarchingStartupSequencePhase.LoadCurrAvailableContracts;
                    }
                    GameLoop.ResumeUpdates(UpdateMasks.ContractSystemsMask);
                }
            }
        }

        // Coordinates with ContractCompletionSystem: trigger it on Waiting, continue on Completed.
        static private void ProcessContractCompletion(OverarchingStartupSequenceState startupState, ContractCompletionState completionState) {
            if (completionState.Phase == ContractCompletionPhase.Waiting) {
                // begin ContractCompletionSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractCompletionSystem");
                completionState.Phase = ContractCompletionPhase.BeginLoadFromPrevChapter;
            }
            else {
                if (completionState.Phase == ContractCompletionPhase.Completed) {
                    // next: load available contracts
                    startupState.Phase = OverarchingStartupSequencePhase.LoadCurrAvailableContracts;
                }
            }
        }

        // Starts loading available contracts, then either opens contract selection or jumps to loading the known-selected contract.
        static private void ProcessLoadCurrAvailableContracts(OverarchingStartupSequenceState startupState, ChapterLoadState chapterLoadState, ContractSelectState selectState, ChapterState chapterState) {
            // start load available contracts
            //Debug.Log("[OverarchingStartupSequenceSystem] ship menu displayed");
            //SpacefabGame.Events.Dispatch(GameEvents.ShipMenuDisplayed);
            chapterLoadState.Phase = ChapterLoadPhase.LoadingAvailableContracts;
            MusicPlayer.SetLoopingTrack("Overarching.Music");

            // If no contract is selected yet, defer to selection; otherwise jump to loading the selected contract.
            if (chapterState.LastSelectedContractIndex == -1) {
                startupState.Phase = OverarchingStartupSequencePhase.ContractSelectSystem;
                selectState.Phase = ContractSelectPhase.Waiting;
                SpacefabGame.Events.Dispatch(GameEvents.OpenContractView);
            }
            else {
                // load selected contract
                startupState.Phase = OverarchingStartupSequencePhase.LoadSelectedContract;
            }
        }

        // Coordinates with ContractSelectSystem after available-contracts load finishes: trigger, then advance to confirm on Completed.
        static private void ProcessContractSelectSystem(OverarchingStartupSequenceState startupState, ChapterLoadState chapterLoadState, ContractSelectState selectState, ContractConfirmState confirmState) {
            // wait for LoadAvailableContracts routine to complete
            if (chapterLoadState.LoadRoutine.Exists() || chapterLoadState.Phase == ChapterLoadPhase.LoadingAvailableContracts) { return; }

            if (selectState.Phase == ContractSelectPhase.Waiting) {
                // begin ContractSelectSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractSelectSystem");
                selectState.Phase = ContractSelectPhase.Loading;
            }
            else {
                if (selectState.Phase == ContractSelectPhase.Completed) {
                    //  confirm selected contract
                    ScriptUtility.Trigger("OnContractSelected");
                    confirmState.Phase = ContractConfirmPhase.Waiting;
                    startupState.Phase = OverarchingStartupSequencePhase.ContractConfirmSystem;
                }
            }
        }

        // Coordinates with ContractConfirmSystem: trigger on Waiting, advance to load on Completed.
        static private void ProcessContractConfirmSystem(OverarchingStartupSequenceState startupState, ContractLoadState contractLoadState, ContractConfirmState confirmState) {
            if (confirmState.Phase == ContractConfirmPhase.Waiting) {
                // begin contract confirmation
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractConfirmSystem");
                confirmState.Phase = ContractConfirmPhase.Confirming;
            }
            else {
                if (confirmState.Phase == ContractConfirmPhase.Completed) {
                    // load selected contract
                    contractLoadState.Phase = ContractLoadPhase.Waiting;
                    startupState.Phase = OverarchingStartupSequencePhase.LoadSelectedContract;
                }
            }
        }

        // Coordinates with ContractLoadSystem: trigger, wait for completion, then finalize startup.
        static private void ProcessLoadSelectedContract(OverarchingStartupSequenceState startupState, ChapterLoadState chapterLoadState, ContractLoadState contractLoadState, ProgressMeterState meterState) {
            if (chapterLoadState.LoadRoutine.Exists() || chapterLoadState.Phase == ChapterLoadPhase.LoadingAvailableContracts) { return; }

            if (contractLoadState.Phase == ContractLoadPhase.Waiting) {
                // begin ContractLoadSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractLoadSystem");
                contractLoadState.Phase = ContractLoadPhase.BeginLoad;
                GameLoop.SuspendUpdates(UpdateMasks.ChapterMask);
            }
            else {
                if (contractLoadState.Phase == ContractLoadPhase.Completed) {
                    // refresh progress meter to update funds and cycles
                    meterState.NeedsRefresh = true;
                    Complete(startupState);
                    GameLoop.SuspendUpdates(UpdateMasks.ContractSystemsMask);
                }
            }
        }

        // Marks startup complete and resumes the overarching scene's normal update mask.
        static private void Complete(OverarchingStartupSequenceState startupState) {
            startupState.Phase = OverarchingStartupSequencePhase.Completed;
            SpacefabGame.Events.Dispatch(GameEvents.ShipMenuDisplayed);
            GameLoop.ResumeUpdates(UpdateMasks.OverarchingMask);
            // Fire the Leaf trigger now that the overarching scene is fully loaded and interactive,
            // letting narrative scripts respond to entering the scene (e.g. gating a node on
            // IsSolutionFoundFor for completed minigames).
            ScriptUtility.Trigger(ScriptTriggers.OnOverarchingLoaded);
            Debug.Log("[OverarchingStartupSequenceSystem] Overarching Startup Sequence Completed");
        }
    }
}

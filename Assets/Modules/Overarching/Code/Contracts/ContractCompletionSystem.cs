using BeauRoutine;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Plays the "return from the previously completed chapter" sequence: loads the previous
    /// chapter's available contracts, animates in and out the completed-contract UI, clears
    /// the last selected contract, and unloads. Runs on Update at order 10 under ContractSystemsMask.
    /// </summary>
    public class ContractCompletionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 10, UpdateMasks.ContractSystemsMask),
                new SysPermissions()
                    .ReadWriteShared<ContractCompletionState>()
                    .ReadWriteShared<ContractLayoutState>()
                    .ReadWriteShared<ChapterState>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        // Steps the completion-sequence phase machine forward.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ContractCompletionState completionState,
                out ContractLayoutState layoutState,
                out ChapterState chapterState
                );

            Find.State(
                out PlayerProgressState progressState
                );

            switch (completionState.Phase) {
                case ContractCompletionPhase.BeginLoadFromPrevChapter:
                    ProcessBeginLoadFromPrevChapter(completionState, layoutState, chapterState);
                    break;
                case ContractCompletionPhase.LoadFromPrevChapter:
                    ProcessLoadFromPrevChapter(completionState, layoutState, chapterState);
                    break;
                case ContractCompletionPhase.EnterPreviousContract:
                    ProcessEnterPrevContract(completionState, layoutState, progressState);
                    break;
                case ContractCompletionPhase.EvaluatePreviousContract:
                    ProcessEvaluatePrevContract(completionState, layoutState);
                    break;
                case ContractCompletionPhase.HidePreviousContract:
                    ProcessHidePrevContract(completionState, layoutState, chapterState);
                    break;
                case ContractCompletionPhase.UnloadFromPrevChapter:
                    ProcessUnloadFromPrevChapter(completionState, layoutState);
                    break;
                default:
                    break;
            }
        }

        // Kicks off the load of the previously-available contracts.
        static private void ProcessBeginLoadFromPrevChapter(ContractCompletionState completionState, ContractLayoutState layoutState, ChapterState chapterState) {
            layoutState.CompletionRoutine.Replace(ContractCompletionUtility.LoadFromPrevChapterRoutine(completionState, chapterState));
            completionState.Phase = ContractCompletionPhase.LoadFromPrevChapter;
        }

        // Once loaded, populates and animates in the completed-contract UI.
        static private void ProcessLoadFromPrevChapter(ContractCompletionState completionState, ContractLayoutState layoutState, ChapterState chapterState) {
            if (!layoutState.CompletionRoutine.Exists()) {
                ContractCompletionUtility.PopulateContractUI(completionState, layoutState, chapterState);
                layoutState.CompletionRoutine.Replace(ContractCompletionUtility.EnterPreviousRoutine(layoutState));
                completionState.Phase = ContractCompletionPhase.EnterPreviousContract;
            }
        }

        // After entry, runs the evaluate routine (currently a placeholder pause).
        static private void ProcessEnterPrevContract(ContractCompletionState completionState, ContractLayoutState layoutState, PlayerProgressState progressState) {
            if (!layoutState.CompletionRoutine.Exists()) {
                layoutState.CompletionRoutine.Replace(ContractCompletionUtility.EvaluatePreviousRoutine(layoutState, progressState));
                completionState.Phase = ContractCompletionPhase.EvaluatePreviousContract;
            }
        }

        // After evaluation, animates the contract back out.
        static private void ProcessEvaluatePrevContract(ContractCompletionState completionState, ContractLayoutState layoutState) {
            if (!layoutState.CompletionRoutine.Exists()) {
                layoutState.CompletionRoutine.Replace(ContractCompletionUtility.HidePreviousRoutine(layoutState));
                completionState.Phase = ContractCompletionPhase.HidePreviousContract;
            }
        }

        // Once hidden, clears the last-selected index and unloads previous-chapter contracts.
        static private void ProcessHidePrevContract(ContractCompletionState completionState, ContractLayoutState layoutState, ChapterState chapterState) {
            if (!layoutState.CompletionRoutine.Exists()) {
                chapterState.LastSelectedContractIndex = -1;
                // Unload
                layoutState.CompletionRoutine.Replace(ContractCompletionUtility.UnloadFromPrevChapterRoutine(completionState, chapterState));
                completionState.Phase = ContractCompletionPhase.UnloadFromPrevChapter;
            }
        }

        // Final phase — wait for unload, then mark the completion sequence finished.
        static private void ProcessUnloadFromPrevChapter(ContractCompletionState completionState, ContractLayoutState layoutState) {
            if (!layoutState.CompletionRoutine.Exists()) {
                // Complete
                completionState.Phase = ContractCompletionPhase.Completed;
            }
        }
    }
}

using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Sequences the "submit chapter → move to the next chapter" flow: defers to the shutdown
    /// subsystem, then advances ChapterState and reloads the main scene.
    /// Runs on LateUpdate at order 10 under ShutdownMask.
    /// </summary>
    public class OverarchingSubmitChapterSequenceSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.LateUpdate, 10, UpdateMasks.ShutdownMask),
                new SysPermissions()
                    .ReadWriteShared<OverarchingSubmitChapterSequenceState>()
                    .ReadWriteShared<ChapterState>()
                    .ReadWriteShared<PlayerProgressState>()
                    .ReadShared<MinigameSaveStates>()
            );
        }

        // Dispatches to the handler for the current submit-chapter phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out OverarchingSubmitChapterSequenceState submitState,
                out ChapterState chapterState,
                out PlayerProgressState progressState,
                out ContractState contractState
                );

            Find.State(
                out MinigameSaveStates saveStates
                );

            switch (submitState.Phase) {
                case OverarchingSubmitChapterPhase.Starting:
                    ProcessStarting(submitState);
                    break;
                case OverarchingSubmitChapterPhase.ShutdownSequenceSystem:
                    ProcessShutdownSequenceSystem(submitState);
                    break;
                case OverarchingSubmitChapterPhase.MoveToNextChapter:
                    ProcessMoveToNextChapter(submitState, chapterState, progressState, contractState, saveStates);
                    break;
                default:
                    break;
            }
        }

        // Entry: ask the shutdown subsystem to start.
        static private void ProcessStarting(OverarchingSubmitChapterSequenceState submitState) {
            submitState.Phase = OverarchingSubmitChapterPhase.ShutdownSequenceSystem;
        }

        // Coordinates with the shutdown subsystem: trigger on Waiting, advance on Complete.
        static private void ProcessShutdownSequenceSystem(OverarchingSubmitChapterSequenceState submitState) {
            submitState.Phase = OverarchingSubmitChapterPhase.MoveToNextChapter;
        }

        // Advance to the next chapter (also saves and reloads the main scene inside the utility).
        static private void ProcessMoveToNextChapter(OverarchingSubmitChapterSequenceState submitState, ChapterState chapterState, PlayerProgressState progressState, ContractState contractState, MinigameSaveStates saveStates) {
            ChapterUtility.LoadNextChapter(chapterState, progressState, contractState, saveStates);
            submitState.Phase = OverarchingSubmitChapterPhase.TransitionComplete;
        }
    }
}

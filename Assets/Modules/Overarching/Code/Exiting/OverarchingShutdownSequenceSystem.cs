using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Drives the overarching shutdown phase before a scene transition: unloads the current
    /// chapter's available-contract assets, then signals ShutdownComplete so upstream systems
    /// can continue. Runs on LateUpdate at order 12 under ShutdownMask.
    /// </summary>
    public class OverarchingShutdownSequenceSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.LateUpdate, 12, UpdateMasks.ShutdownMask),
                new SysPermissions()
                    .ReadWriteShared<OverarchingShutdownSequenceState>()
                    .ReadShared<ChapterState>()
                    .ReadWriteShared<AvailableContractsLookup>()
            );
        }

        // Dispatches to the handler for the current shutdown phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out OverarchingShutdownSequenceState shutdownState,
                out ChapterState chapterState,
                out AvailableContractsLookup availableLookup
                );

            switch (shutdownState.Phase) {
                case OverarchingShutdownPhase.BeginShutdown:
                    ProcessBeginShutdown(shutdownState, chapterState, availableLookup);
                    break;
                case OverarchingShutdownPhase.ShuttingDown:
                    ProcessShuttingDown(shutdownState);
                    break;
                default:
                    break;
            }
        }

        // Kicks off the unload of the current chapter's available-contract assets.
        static private void ProcessBeginShutdown(OverarchingShutdownSequenceState shutdownState, ChapterState chapterState, AvailableContractsLookup availableLookup) {
            shutdownState.ShutdownRoutine.Replace(ContractsLookupUtility.UnloadAvailableContractsAtChapter(availableLookup, chapterState, chapterState.CurrChapterIndex));
            shutdownState.Phase = OverarchingShutdownPhase.ShuttingDown;
        }

        // Wait for the unload routine to finish, then flag completion.
        static private void ProcessShuttingDown(OverarchingShutdownSequenceState shutdownState) {
            if (!shutdownState.ShutdownRoutine.Exists()) {
                shutdownState.Phase = OverarchingShutdownPhase.ShutdownComplete;
            }
        }
    }
}

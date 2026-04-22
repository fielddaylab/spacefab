using FieldDay;
using FieldDay.Systems;
using SpaceFab.Overarching;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Loads the current chapter's assets and its available-contracts bundle.
    /// Runs in Update under ChapterMask; drives ChapterLoadState.Phase by kicking off
    /// the appropriate LoadRoutine for each phase.
    /// </summary>
    public class ChapterLoadSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ChapterMask),
                new SysPermissions()
                    .ReadWriteShared<ChapterLoadState>()
                    .ReadShared<ChapterState>()
                    .ReadWriteShared<AvailableContractsLookup>()
            );
        }

        // Advances the chapter-load phase by starting the corresponding load routine.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ChapterLoadState loadState,
                out ChapterState chapterState,
                out AvailableContractsLookup availableLookup
                );

            switch (loadState.Phase) {
                case ChapterLoadPhase.LoadingChapter:
                    if (!loadState.LoadRoutine.Exists()) {
                        // load curr chapter
                        loadState.LoadRoutine.Replace(ChapterLoadUtility.LoadCurrChapter(chapterState, loadState));
                    }
                    break;
                case ChapterLoadPhase.LoadingAvailableContracts:
                    if (!loadState.LoadRoutine.Exists()) {
                        // load chapter in parallel with other systems
                        loadState.LoadRoutine.Replace(ChapterLoadUtility.LoadCurrAvailableContracts(chapterState, loadState, availableLookup));
                    }
                    break;
                default:
                    break;
            }
        }
    }
}

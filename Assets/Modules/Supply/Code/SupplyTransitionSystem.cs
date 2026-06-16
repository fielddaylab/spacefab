using FieldDay;
using FieldDay.Music;
using FieldDay.Scripting;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Handles post-load setup for the Supply minigame.
    /// Remains in SetupMask until the supply chain map scene has loaded,
    /// then transitions to SupplyMask.
    /// Runs on Update phase at order 0 under SetupMask.
    /// </summary>
    public class SupplyTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadWriteShared<SupplyTransitionState>()
                    .ReadShared<ChapterState>()
                    .ReadWriteShared<SupplyChainMap>()
                    .ReadWriteShared<SupplyMinigameState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out SupplyTransitionState transitionState,
                out ChapterState chapterState,
                out SupplyChainMap lookup,
                out SupplyMinigameState supplyState
            );

            switch (transitionState.Phase) {
                case SupplyTransitionPhase.LoadingChapterMap:
                    if (!transitionState.LoadRoutine.Exists()) {
                        transitionState.LoadRoutine.Replace(
                            SupplyChainUtility.LoadChapterMap(lookup, supplyState, transitionState, chapterState.CurrChapterIndex)
                        );
                    }
                    break;
                case SupplyTransitionPhase.Completed:
                    GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
                    GameLoop.ResumeUpdates(UpdateMasks.SupplyMask);

                    ScriptUtility.Trigger(SupplyScriptTriggers.OnSupplySetupCompleted);
                    break;
            }
        }
    }
}

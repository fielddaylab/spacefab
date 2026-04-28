using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Handles post-load setup and post-attempt teardown.
    /// Runs on Update phase at order 0, no category mask.
    /// </summary>
    public class FabricationTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadWriteShared<FabricationTransitionState>()
                    .ReadShared<PlayerProgressState>()
                    .ReadWriteShared<SequenceState>()
            );
        }

        // Reacts to the current exit-request state: shows the modal on request, hides it on confirm.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out FabricationTransitionState transitionState,
                out ModeState modeState,
                out PlayerProgressState playerProgress,
                out SequenceState sequenceState
                );

            Log.Msg("[FabricationTransitionSystem] Setting up level...");
            // setup
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(playerProgress.ContractAssetsWrapperId);
            if (contractAssets == null) {
                Log.Error("FabricationTransistionSystem] Tried to load contract assets but returned null!");
            }
            sequenceState.Level = contractAssets.FabricationLevel;
            Log.Msg("[FabricationTransitionSystem] Setup complete!");

            // Enter Pre-Attempt Mode
            Log.Msg("[FabricationTransitionSystem] Transitioning to PreAttempt Mode");
            ModeUtility.SetNewMode(modeState, LevelMode.PreAttempt);
            GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
            GameLoop.ResumeUpdates(UpdateMasks.PreAttemptMask);
        }
    }
}

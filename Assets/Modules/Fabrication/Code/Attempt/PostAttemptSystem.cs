using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Manages the player's current attempt (from timer start to timer end).
    /// Runs on Update phase at order 0, no category mask. Currently a stub.
    /// </summary>
    public class PostAttemptSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<ModeState>()
                    .ReadWriteShared<ResultDisplayState>()
            );
        }

        // TODO: implement post attempt sequence progression.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ModeState modeState,
                out ResultDisplayState displayState
                );

            if (modeState.CurrMode != LevelMode.PostAttempt) { return; }

            if (modeState.ChangedModeThisFrame)
            {
                // display results
                Log.Msg("[PostAttemptSystem] displaying results");
                displayState.DisplayRequestedThisFrame = true;
            }
        }
    }
}

using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Robot;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Manages the lead-in to a new player attempt
    /// Runs on Update phase at order 0, no category mask. Currently a stub.
    /// </summary>
    public class AttemptLeadInSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.AttemptLeadInMask),
                new SysPermissions()
                    .ReadShared<ModeState>()
            );
        }

        // TODO: implement attempt sequence progression.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ModeState modeState
                );

            if (modeState.CurrMode != LevelMode.AttemptLeadIn) { return; }

            if (modeState.ChangedModeThisFrame)
            {
                // setup Attempt:
                // generate wafer
                Log.Msg("[AttemptLeadInSystem] TODO: generating wafer");

                // initiate countdown
                Log.Msg("[AttemptLeadInSystem] TODO: running countdown");
            }
        }
    }
}

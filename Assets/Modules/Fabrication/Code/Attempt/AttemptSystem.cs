using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Manages the player's current attempt (from timer start to timer end).
    /// Runs on Update phase at order 0, no category mask. Currently a stub.
    /// </summary>
    public class AttemptSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<SequenceState>()
            );
        }

        // TODO: implement attempt sequence progression.
        static private void ProcessWork(float deltaTime) {

        }
    }
}

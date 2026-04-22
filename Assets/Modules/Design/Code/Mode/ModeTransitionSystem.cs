using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Facilitates transitioning between Design minigame modes (Tool vs. Simulate).
    /// Runs on Update at order 0, no category mask. Currently a stub.
    /// </summary>
    public class ModeTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<ModeTransitionState>()
            );
        }

        // TODO: implement mode transition logic.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

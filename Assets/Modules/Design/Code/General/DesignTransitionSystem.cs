using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Manages transitioning into and out of the Design minigame scene.
    /// Sets up and shuts down relevant systems. Runs on Update at order 0, no category mask. Currently a stub.
    /// </summary>
    public class DesignTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<DesignTransitionState>()
            );
        }

        // TODO: implement design transition logic.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

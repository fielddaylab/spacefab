using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Displays results after evaluating the player's design.
    /// Runs on Update at order 0, no category mask. Currently a stub.
    /// </summary>
    public class ResultSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<ResultState>()
            );
        }

        // TODO: implement results display.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

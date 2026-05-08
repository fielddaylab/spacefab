using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Manages results display after an attempt is completed.
    /// Runs on Update phase at order 1 under PostAttemptMask. Currently a stub.
    /// </summary>
    public class ResultSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 1, UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<ResultDisplayState>()
            );
        }

        // TODO: implement results display.
        static private void ProcessWork(float deltaTime) {
            Find.State(out ResultDisplayState displayState);

            if (displayState.DisplayRequestedThisFrame)
            {
                ResultDisplayStateUtility.ShowResults(displayState);
            }
        }
    }
}

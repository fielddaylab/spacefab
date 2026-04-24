using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// </summary>
    public class CountdownRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.AttemptLeadInMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<CountdownState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(out CountdownState countdownState);

            countdownState.CountdownRequestedThisFrame = false;
            countdownState.CountdownCompletedThisFrame = false;
        }
    }
}

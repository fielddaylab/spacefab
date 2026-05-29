using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// </summary>
    public class InterruptRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 10, UpdateMasks.AttemptLeadInMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<InterruptState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(out InterruptState interruptState);

            interruptState.ResetRequestedThisFrame = false;
            interruptState.RestoreCheckpointRequestedThisFrame = false;
            interruptState.FinalizeAttemptRequestedThisFrame = false;
        }
    }
}

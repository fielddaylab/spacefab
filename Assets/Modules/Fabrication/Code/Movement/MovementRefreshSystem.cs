using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement {
    /// <summary>
    /// Clears MovementState's one-frame SlotChangedThisFrame flag at end of frame, after the
    /// station-control state machine has consumed it. Runs on LateUpdate at order 100 under AttemptMask.
    /// </summary>
    public class MovementRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<MovementState>()
            );
        }

        // Clears the slot-change flag so it is only visible for one frame.
        static private void ProcessWork(float deltaTime) {
            Find.State<MovementState>().SlotChangedThisFrame = false;
        }
    }
}

using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.StationControl {
    /// <summary>
    /// Clears StationControlState's one-frame flags (MicrogameCompletedThisFrame, CancelRequestedThisFrame,
    /// MicrogamePassedThisFrame) at end of frame, so all Update-phase consumers get one-frame visibility
    /// before they are cleared. Runs on LateUpdate at order 100 under AttemptMask.
    /// </summary>
    public class StationControlFlagRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<StationControlState>()
            );
        }

        // Clears the one-frame flags on StationControlState.
        static private void ProcessWork(float deltaTime) {
            StationControlState stationState = Find.State<StationControlState>();
            stationState.MicrogameCompletedThisFrame = false;
            stationState.CancelRequestedThisFrame = false;
            stationState.MicrogamePassedThisFrame = false;
        }
    }
}

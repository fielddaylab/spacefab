using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement
{
    /// <summary>
    /// Clears ModeState's per-frame flags.
    /// Runs on LateUpdate at order 100 under AttemptMask.
    /// </summary>
    public class ModeRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.PreAttemptMask | UpdateMasks.AttemptLeadInMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<ModeState>()
            );
        }

        // Clears the slot-change flag so it is only visible for one frame.
        static private void ProcessWork(float deltaTime)
        {
            Find.State<ModeState>().ChangedModeThisFrame = false;
        }
    }
}

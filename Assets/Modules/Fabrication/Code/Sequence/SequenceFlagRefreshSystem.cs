using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// Clears SequenceState's MisalignmentThisFrame one-frame flag at end of frame, so all
    /// Update-phase consumers see it for exactly one frame. Runs on LateUpdate at order 100 under
    /// AttemptMask.
    /// </summary>
    public class SequenceFlagRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<SequenceState>()
            );
        }

        // Clears the one-frame misalignment flag.
        static private void ProcessWork(float deltaTime)
        {
            Find.State<SequenceState>().MisalignmentThisFrame = false;
        }
    }
}

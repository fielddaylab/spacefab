using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Drives the Ion Implanter microgame's per-frame simulation while it is active. Runs on
    /// Update at order 0 under AttemptMask; gated by IonMicrogameState.IsActive.
    /// </summary>
    public class IonMicrogameSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<IonMicrogameState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out IonMicrogameState state);
            if (!state.IsActive) { return; }

            // TODO: drive the Ion Implanter mechanics once defined.
        }
    }
}

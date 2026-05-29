using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Clears TutorialInterruptState's one-frame request flag (TutorialInterruptRequested) at end of frame,
    /// after TutorialInterruptSystem has consumed it. RestartButtonPressedThisFrame is deliberately NOT
    /// cleared here — it's a latch set by the async restart-button click and is consumed-and-cleared by
    /// TutorialInterruptSystem, so clearing it on a fixed frame boundary could drop a click. Leaves the
    /// persistent ListenerRegistered guard intact. Runs on LateUpdate at order 100 under AttemptMask.
    /// </summary>
    public class TutorialInterruptRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<TutorialInterruptState>()
            );
        }

        static private void ProcessWork(float deltaTime)
        {
            Find.State(out TutorialInterruptState interruptState);

            interruptState.TutorialInterruptRequested = false;
        }
    }
}

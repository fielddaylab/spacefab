using FieldDay;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Drives the Fabrication Timer's time and text update, runs on FixedUpdate.
    /// at order 0 under AttemptMask; gated by TimeState.IsPaused.
    /// </summary>
    public class TimeTickSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.FixedUpdate, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<TimeState>()
                );
        }

        // Early returns when timer is paused or if time is greater than display space
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out TimeState state);

            if (state.IsPaused || state.ElapsedTime >= 99.99f) return;

            state.ElapsedTime += deltaTime;
            if (state.ElapsedTime > 99.99f)
            {
                state.ElapsedTime = 99.99f;
            }

            // convert float time to readable format
            string result = state.ElapsedTime.ToString("00.00");
            state.TimerText.text = result;
        }
    }
}
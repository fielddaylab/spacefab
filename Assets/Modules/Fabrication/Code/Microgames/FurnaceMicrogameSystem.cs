using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Drives the Furnace microgame's per-frame simulation while it is active. Runs on FixedUpdate
    /// at order 0 under AttemptMask; gated by FurnaceMicrogameState.IsActive.
    /// </summary>
    public class FurnaceMicrogameSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.FixedUpdate, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<FurnaceMicrogameState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out FurnaceMicrogameState microgameState);
            if (!microgameState.IsActive) { return; }

            switch (microgameState.Phase)
            {
                case FurnaceMicrogamePhase.Idle:
                    break;
                case FurnaceMicrogamePhase.Entering:
                    break;
                case FurnaceMicrogamePhase.Burning:
                    ProcessingBurning(microgameState, deltaTime);
                    break;
                case FurnaceMicrogamePhase.Fueling:
                    ProcessFueling(microgameState, deltaTime);
                    break;
                default:
                    break;
            }
        }

            // TODO: read Activate-hold input; integrate heat value toward target; clamp to range.

        #region Helpers

        static bool keyDown = false;
        private static void ProcessingBurning(FurnaceMicrogameState state, float deltaTime)
        {
            if (state.InputAccepted && Game.Input.IsKeyDown(FabricationConsts.Activate)) {
                state.CurrentValue += state.Sensitivity * deltaTime;
                keyDown = true;
            }
            if (state.InputAccepted && Game.Input.IsKeyUp(FabricationConsts.Activate))
            {
                state.Phase = FurnaceMicrogamePhase.Fueling;
                Debug.Log(state.CurrentValue);
            }
        }

        private static void ProcessFueling(FurnaceMicrogameState state, float deltaTime)
        {
            
        }

        #endregion // Helpers
    }
}

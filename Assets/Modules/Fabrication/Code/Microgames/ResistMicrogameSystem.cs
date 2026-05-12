using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.StationControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Drives the Photoresist microgame's per-frame simulation while it is active. Runs on
    /// FixedUpdate at order 0 under AttemptMask; gated by ResistMicrogameState.IsActive.
    /// </summary>
    public class ResistMicrogameSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.FixedUpdate, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<ResistMicrogameState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out ResistMicrogameState state);
            if (!state.IsActive) { return; }


            // TODO: sweep dropper position; on Activate-press, record drop X and signal completion.
            state.SweeperX = state.MaxOffset * Mathf.Sin(Time.time * state.SweepSpeed) + state.CenterX;

            Vector3 SweeperPosition = state.SweeperGraphic.position;
            SweeperPosition.x = state.SweeperX;
            state.SweeperGraphic.position = SweeperPosition;

            if (state.InputAccepted && Game.Input.IsKeyPressed(FabricationConsts.Activate))
            {
                state.DropX = state.SweeperX;

                Find.State(out StationControlState stationState);
                stationState.ActiveInterfacer.CompletedThisFrame = true;
            }
        }
    }
}

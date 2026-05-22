using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.StationControl;
using SpaceFab.Fabrication.Stations;
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
            Find.State(
                out FurnaceMicrogameState microgameState, 
                out MicrogameCanvasState canvasState // use for enabling/disabling fader and popups
            );

            if (!microgameState.IsActive) { return; }

            switch (microgameState.Phase)
            {
                case FurnaceMicrogamePhase.Idle:
                    break;
                case FurnaceMicrogamePhase.Entering:
                    canvasState.ShowUI(FabricationConsts.FURNACE_STATION_ID);
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

        // increments current value for duration player holds activation key, then sets final heat to value on release
        private static void ProcessingBurning(FurnaceMicrogameState state, float deltaTime)
        {
            if (state.InputAccepted && Game.Input.IsKeyDown(FabricationConsts.Activate)) {
                state.CurrentValue += state.Sensitivity * deltaTime;
            }
            if (state.InputAccepted && Game.Input.IsKeyUp(FabricationConsts.Activate))
            {
                state.Phase = FurnaceMicrogamePhase.Fueling;
                state.FinalHeat = state.CurrentValue;
            }
        }

        // animates movement of the meter arrow to the final heat position, then triggers exit of microgame
        private static void ProcessFueling(FurnaceMicrogameState state, float deltaTime)
        {
            float targetPercentage = state.FinalHeat / state.MaxRange;
            float targetZRotation = -targetPercentage * 180;
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, targetZRotation));

            // frame independent smoothing, probably make less expensive later
            float blend = 1f - Mathf.Pow(state.MeterSmoothing, deltaTime);

            state.MeterArrowAnchor.rotation = Quaternion.Slerp(
                state.MeterArrowAnchor.rotation, 
                targetRotation, 
                blend
            );

            if (Quaternion.Angle(state.MeterArrowAnchor.rotation, targetRotation) < 0.1f) 
            {
                Find.State(out StationControlState stationState);
                MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
            }
        }

        #endregion // Helpers
    }
}

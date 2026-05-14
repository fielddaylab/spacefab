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
                    .ReadWriteShared<MicrogameCanvasState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        private static void ProcessWork(float deltaTime)
        {
            Find.State(
                out ResistMicrogameState microgameState,
                out MicrogameCanvasState canvasState // use for enabling/disabling fader and popups
                );
            if (!microgameState.IsActive) { return; }

            switch (microgameState.Phase)
            {
                case ResistMicrogamePhase.Idle:
                    break;
                case ResistMicrogamePhase.Entering:
                    break;
                case ResistMicrogamePhase.Sweeping:
                    ProcessSweeping(microgameState);
                    break;
                case ResistMicrogamePhase.Spreading:
                    ProcessSpreading(microgameState, deltaTime);
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        // sweep dropper position; on Activate-press, record drop X and signal completion.
        private static void ProcessSweeping(ResistMicrogameState state)
        {
            // sweep dropper positional function
            state.SweeperX = state.MaxOffset * Mathf.Sin(Time.time * state.SweepSpeed) + state.CenterX;

            Vector3 SweeperPosition = state.SweeperAnchor.localPosition;
            SweeperPosition.x = state.SweeperX;
            state.SweeperAnchor.localPosition = SweeperPosition;

            // on player input, capture drop position and begin spreading phase
            if (state.InputAccepted && Game.Input.IsKeyPressed(FabricationConsts.Activate))
            {
                state.DropX = state.SweeperX;

                // enable and set up spreading graphic
                state.SpreadingGraphic.localScale = Vector3.zero;
                state.SpreadingGraphic.gameObject.SetActive(true);

                Vector3 spreadPosition = state.SpreadingGraphic.localPosition;
                spreadPosition.x = state.DropX;
                state.SpreadingGraphic.localPosition = spreadPosition;

                accruedSpread = 0;

                state.Phase = ResistMicrogamePhase.Spreading;
            }
        }

        // animate a circle spreading over the chip starting at drop X; exit microgame once complete
        static float accruedSpread = 0;
        private static void ProcessSpreading(ResistMicrogameState state, float deltaTime)
        {
            accruedSpread += deltaTime * state.SpreadingSpeed;

            state.SpreadingGraphic.transform.localScale = Vector3.one * accruedSpread;

            // animation finished, exit out
            if (accruedSpread >= 1f)
            {
                Find.State(out StationControlState stationState);
                MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
            }
        }

        #endregion // Helpers
    }
}

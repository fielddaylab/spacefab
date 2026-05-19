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
    /// Drives the Ion Implanter microgame's per-frame simulation while it is active. Runs on
    /// FixedUpdate at order 0 under AttemptMask; gated by IonMicrogameState.IsActive.
    /// </summary>
    public class IonMicrogameSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.FixedUpdate, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<IonMicrogameState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out IonMicrogameState state, 
                out MicrogameCanvasState canvasState // use for enabling/disabling fader and popups
            );
            if (!state.IsActive) { return; }

            if (state.Phase == IonMicrogamePhase.Filling) {
                Vector2 mousePosition = Game.Rendering.PrimaryCamera.ScreenToWorldPoint(Input.mousePosition);
                state.DropperAnchor.position = mousePosition;

                // TODO: drive the Ion Implanter mechanics once defined.
                if (state.InputAccepted) state.IonPattern.ProcessWork();
                if (state.IonPattern.CompletelyFilled)
                {
                    Find.State(out StationControlState stationState);
                    MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
                }
            }

            state.IonPattern.PerformRendering();
        }
    }
}

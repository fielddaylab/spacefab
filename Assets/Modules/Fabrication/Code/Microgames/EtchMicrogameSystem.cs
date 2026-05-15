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
    /// Drives the Plasma Etcher microgame's per-frame simulation while it is active. Runs on
    /// FixedUpdate at order 0 under AttemptMask; gated by EtchMicrogameState.IsActive.
    /// </summary>
    public class EtchMicrogameSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.FixedUpdate, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<EtchMicrogameState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out EtchMicrogameState state, out MicrogameCanvasState canvasState);
            if (!state.IsActive) { return; }

            switch (state.Phase)
            {
                case EtchMicrogamePhase.Entering:
                    canvasState.ShowUI(FabricationConsts.ETCH_STATION_ID);
                    ProcessEntering(state, deltaTime);
                    break;

                case EtchMicrogamePhase.Active:
                    ProcessActive(state, deltaTime);
                    break;
            }

            // TODO: read directional input; advance beam over pattern; tally correct/incorrect cells; detect wafer-exit.
        }

        static private void ProcessEntering(EtchMicrogameState state, float deltaTime)
        {
            if (state.PreviewPoints.Count == 0)
                return;

            float beamSpeed = 20f;
            state.PreviewProgress += deltaTime * beamSpeed;

            int visibleCount = Mathf.Clamp(Mathf.FloorToInt(state.PreviewProgress),
                0, state.PreviewPoints.Count);
            
            // Debug.Log($"Etch microgame entering: progress={state.PreviewProgress} visibleCount={visibleCount}");

            if (visibleCount != state.PreviewVisibleCount)
            {
                state.PreviewVisibleCount = visibleCount;
                state.PreviewBeam.positionCount = visibleCount;

                for (int i = 0; i < visibleCount; i++)
                {
                    state.PreviewBeam.SetPosition(i, state.PreviewPoints[i]);
                }
            }

            if (state.PreviewVisibleCount >= state.PreviewPoints.Count)
            {
                state.Phase = EtchMicrogamePhase.Active;
                // Debug.Log($"Etch Enter Complete, {state.PreviewPoints.Count} preview points visible");
            }
        }

        static private void ProcessActive(EtchMicrogameState state, float deltaTime)
        {
            if (!state.InputAccepted)
                return;

            if (Game.Input.IsKeyPressed(FabricationConsts.Up0) || Game.Input.IsKeyPressed(FabricationConsts.Up1))
                state.Direction = Vector2.up;
            else if (Game.Input.IsKeyPressed(FabricationConsts.Down0) || Game.Input.IsKeyPressed(FabricationConsts.Down1))
                state.Direction = Vector2.down;
            else if (Game.Input.IsKeyPressed(FabricationConsts.Left0) || Game.Input.IsKeyPressed(FabricationConsts.Left1))
                state.Direction = Vector2.left;
            else if (Game.Input.IsKeyPressed(FabricationConsts.Right0) || Game.Input.IsKeyPressed(FabricationConsts.Right1))
                state.Direction = Vector2.right;

            Vector3 current = state.PlayerPoints[state.PlayerPoints.Count - 1];

            Vector3 next = current + (Vector3)(state.Direction * state.BeamSpeed * deltaTime);

            if (next.sqrMagnitude > state.WaferRadius * state.WaferRadius)
            {
                Find.State(out StationControlState stationState);
                MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
                return;
            }

            state.PlayerPoints.Add(next);
            state.PlayerBeam.positionCount = state.PlayerPoints.Count;
            state.PlayerBeam.SetPosition(state.PlayerPoints.Count - 1, next);
        }
    }
}

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
    /// Drives the Sputter microgame's per-frame simulation while it is active. Runs on FixedUpdate
    /// at order 0 under AttemptMask; gated by SputterMicrogameState.IsActive.
    /// </summary>
    public class SputterMicrogameSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.FixedUpdate, 0, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<SputterMicrogameState>()
            );
        }

        // Early-returns when the microgame is not active. Active body is TODO until mechanics are authored.
        static private void ProcessWork(float deltaTime)
        {
            Find.State(out SputterMicrogameState state, out MicrogameCanvasState canvasState);
            if (!state.IsActive) { return; }

            switch (state.Phase)
            {
                case SputterMicrogamePhase.Entering:
                    MicrogameCanvasUtility.ShowStationInstructions(canvasState, FabricationConsts.SPUTTER_STATION_ID);
                    ProcessBeamAnimation(state, deltaTime);
                    break;
                case SputterMicrogamePhase.Active:
                    ProcessBeamAnimation(state, deltaTime);
                    ProcessActive(state, deltaTime);
                    break;
            }
        }

        static private void ProcessBeamAnimation(SputterMicrogameState state, float deltaTime)
        {
            Vector2 offset = state.IncidentBeam.material.mainTextureOffset;
            offset.x -= Time.deltaTime;
            state.IncidentBeam.material.mainTextureOffset = offset;

            float scale = 1; // 1.5f;
            foreach (LineRenderer lr in state.ReflectedBeam)
            {
                lr.material.mainTextureOffset = offset * scale;
            }
        }

        static private void ProcessActive(SputterMicrogameState state, float deltaTime)
        {
            if (!state.InputAccepted)
                return;

            Vector3 delta = Vector2.zero;
            if (Game.Input.IsKeyDown(FabricationConsts.Left0) || Game.Input.IsKeyDown(FabricationConsts.Left1))
            {
                if (state.SputterSprites.localPosition.x > 0f)
                    delta = Vector2.left * Time.deltaTime;
            }
            else if (Game.Input.IsKeyDown(FabricationConsts.Right0) || Game.Input.IsKeyDown(FabricationConsts.Right1))
            {
                delta = Vector2.right * Time.deltaTime;
            }

            state.SputterSprites.localPosition += delta;

            float scale = 1; // 1.5f;
            state.PatternRenderer.size = new Vector2(state.PatternRenderer.size.x + delta.x * scale, state.PatternRenderer.size.y);
            state.PatternAccumulatedX += delta.x * scale / 2;
            state.PatternRenderer.transform.localPosition = new Vector3(state.PatternStartX + state.PatternAccumulatedX, 0f, 0f);

            if (state.SputterSprites.localPosition.x > state.MaxSputterDistance)
            {
                Find.State(out StationControlState stationState);
                MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
                return;
            }
        }
    }
}

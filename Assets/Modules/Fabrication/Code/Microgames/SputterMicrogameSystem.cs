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
                    break;
                case SputterMicrogamePhase.Active:
                    ProcessActive(state, deltaTime);
                    break;
            }
        }

        private static float spawnTimer = 0.5f;
        static private void ProcessActive(SputterMicrogameState state, float deltaTime)
        {
            if (!state.InputAccepted)
                return;
            
            float angle = state.SputterSprites.eulerAngles.z;

            if (Game.Input.IsKeyDown(FabricationConsts.Left0) || Game.Input.IsKeyDown(FabricationConsts.Left1))
            {
                angle += deltaTime * 50f;
            }
            else if (Game.Input.IsKeyDown(FabricationConsts.Right0) || Game.Input.IsKeyDown(FabricationConsts.Right1))
            {
                angle -= deltaTime * 50f;
            }
            state.SputterSprites.rotation = Quaternion.Euler(0, 0, angle);

            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = 0.5f;
                SputterMicrogameProjectile projectile = Instantiate(state.SputterProjectilePrefab, state.ProjectileParent);
                projectile.transform.position = state.InitialPos.position;
                projectile.SetDirection(angle);
            }



            float scale = 1.5f;
            //state.PatternRenderer.size = new Vector2(state.PatternRenderer.size.x + delta.x * scale, state.PatternRenderer.size.y);
            //state.PatternRenderer.transform.localPosition += new Vector3(delta.x * scale / 2, 0f, 0f);

            if (state.SputterSprites.localPosition.x > state.MaxSputterDistance)
            {
                Find.State(out StationControlState stationState);
                MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
                return;
            }
        }
    }
}

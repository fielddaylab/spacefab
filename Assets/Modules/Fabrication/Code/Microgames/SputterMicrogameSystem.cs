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

        private static float spawnTimer = 0f;
        static private void ProcessActive(SputterMicrogameState state, float deltaTime)
        {
            if (!state.InputAccepted)
                return;
            
            float angle = state.SputterHeadAnchor.eulerAngles.z;
            float rotationSpeed = 15f;

            if (Game.Input.IsKeyDown(FabricationConsts.Left0) || Game.Input.IsKeyDown(FabricationConsts.Left1))
            {
                angle = Mathf.Min(90f, angle + deltaTime * rotationSpeed);
            }
            else if (Game.Input.IsKeyDown(FabricationConsts.Right0) || Game.Input.IsKeyDown(FabricationConsts.Right1))
            {
                angle = Mathf.Max(0f, angle - deltaTime * rotationSpeed);
            }
            state.SputterHeadAnchor.rotation = Quaternion.Euler(0, 0, angle);

            // Spawn projectile
            Vector2 startPosition = state.FirePoint.position;
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = 0.2f;
                SputterMicrogameProjectile projectile = Instantiate(state.ProjectilePrefab, state.ProjectileParent);
                projectile.transform.position = startPosition;
                // direction
                projectile.SetDirection(angle);
            }

            // Update line renderer (trajectory preview)
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            RaycastHit2D hit = Physics2D.Raycast(startPosition, direction, 100f, ~(1 << 2));
            state.TrajectoryPreview.SetPosition(0, startPosition);

            state.TrajectoryPreview.positionCount = 2;
            state.TrajectoryPreview.SetPosition(1, hit.point);
            if (hit.collider.name == "Mirror")
            {
                state.TrajectoryPreview.positionCount++;
                direction = Quaternion.Euler(0, 0, -angle) * Vector2.right;
                hit = Physics2D.Raycast(hit.point + direction * 0.01f, direction, 100f, ~(1 << 2));
                state.TrajectoryPreview.SetPosition(2, hit.point);
            }

            if (state.SputterPattern.CompletelyFilled)
            {
                Find.State(out StationControlState stationState);
                MicrogameStationInterfacerUtility.SignalCompleted(stationState.ActiveInterfacer);
            }
        }
    }
}

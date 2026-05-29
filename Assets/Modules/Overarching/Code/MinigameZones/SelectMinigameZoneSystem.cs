using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// PointerEvents get set AFTER Update. So this system checks for them before update,
    /// then refreshes the fields on Update (see MinigameZoneRefreshSystem)
    /// </summary>
    public class SelectMinigameZoneSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
			ecs.Register(&ProcessWork,
				new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.OverarchingMask),
				new SysPermissions()
					.ReadWrite<MinigameZone>()
					.ReadWriteShared<MinigameZonesState>()
					.ReadShared<OverarchingAlertState>()
			);
        }

        static private void ProcessWork(float deltaTime)
        {
            MinigameZonesState state = Find.State<MinigameZonesState>();
            OverarchingAlertState alertState = Find.State<OverarchingAlertState>();
            MinigameZoneOverlayDB overlayDB = Find.GlobalAsset<MinigameZoneOverlayDB>();

			var components = Find.Components<MinigameZone>();

			for(int i = 0; i < components.Count; i++) {
				MinigameZone zone = components[i];
				if (IsZoneLocked(alertState, zone)) { continue; }
				if (zone.PointerExitThisFrame) {
					MinigameZonesUtility.CancelHover(state, zone.ZoneIndex);
				}
			}

			for (int i = 0; i < components.Count; i++) {
				MinigameZone zone = components[i];
				if (IsZoneLocked(alertState, zone)) { continue; }
				if (zone.PointerEnterThisFrame)
                {
                    MinigameZonesUtility.BeginHover(state, zone.ZoneIndex);
                }
            }

			for (int i = 0; i < components.Count; i++) {
				MinigameZone zone = components[i];
				if (IsZoneLocked(alertState, zone)) { continue; }
				if (zone.ClickedThisFrame)
                {
                    MinigameZonesUtility.ClickZone(state, zone.ZoneIndex);
                }
            }

            // Apply each zone's resting + hover overlay every frame, so unlocked zones always show
            // their non-focus overlay (and react to lock / needs-attention changes) regardless of
            // whether a pointer event fired this frame.
            MinigameZonesUtility.RefreshZoneVisuals(state, alertState, overlayDB);
        }

        // True when the zone's alert mask has the Locked bit set. Locked zones get no hover
        // highlight, no selection highlight, no click-through to the minigame — they're
        // visually frozen, with only the lock icon (spawned by OverarchingAlertSystem) signaling
        // unavailability. Pointer one-frame flags still tick down via MinigameZoneRefreshSystem,
        // so no leftover state lingers when Locked clears.
        static private bool IsZoneLocked(OverarchingAlertState alertState, MinigameZone zone)
        {
            return OverarchingAlertUtility.HasAlert(alertState, zone.Minigame, AlertType.Locked);
        }
    }
}
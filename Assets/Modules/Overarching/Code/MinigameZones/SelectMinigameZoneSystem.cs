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
    [SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.OverarchingMask)]
    public class SelectMinigameZoneSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
			ecs.Register(&ProcessWork,
				new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.OverarchingMask),
				new SysPermissions()
					.ReadWrite<MinigameZone>()
					.ReadWriteShared<MinigameZonesState>()
					.ReadShared<PaletteState>()
			);
        }

        static private void ProcessWork(float deltaTime)
        {
            MinigameZonesState state = Find.State<MinigameZonesState>();
            PaletteState palette = Find.State<PaletteState>();

			var components = Find.Components<MinigameZone>();

			for(int i = 0; i < components.Count; i++) {
				MinigameZone zone = components[i];
				if (zone.PointerExitThisFrame) {
					MinigameZonesUtility.CancelHover(state, zone.ZoneIndex);
				}
			}

			for (int i = 0; i < components.Count; i++) {
				MinigameZone zone = components[i];
				if (zone.PointerEnterThisFrame)
                {
                    MinigameZonesUtility.BeginHover(state, palette, zone.ZoneIndex);
                }
            }

			for (int i = 0; i < components.Count; i++) {
				MinigameZone zone = components[i];
				if (zone.ClickedThisFrame)
                {
                    MinigameZonesUtility.ClickZone(state, palette, zone.ZoneIndex);
                }
            }
        }
    }
}
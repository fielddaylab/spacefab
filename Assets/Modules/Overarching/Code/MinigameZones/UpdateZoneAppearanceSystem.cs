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
    public class UpdateZoneAppearanceSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
			ecs.Register(&ProcessWork,
				new SysUpdate(GameLoopPhase.LateUpdate, -1),
				new SysPermissions()
					.ReadWrite<MinigameZone>()
					.ReadWriteShared<MinigameZonesState>()
			);
        }

        static private void ProcessWork(float deltaTime)
        {
            Find.State(out MinigameZonesState state);

            if (state.StatusDirty) {
                state.StatusDirty = false;

                MinigameZonesUtility.UpdateAllZoneAppearances(state);
            }
        }
    }
}
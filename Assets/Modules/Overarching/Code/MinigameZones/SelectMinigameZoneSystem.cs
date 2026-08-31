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
            );
        }

        static private void ProcessWork(float deltaTime)
        {
            MinigameZonesState state = Find.State<MinigameZonesState>();

            if (state.QueuedZone != null) {
                MinigameZonesUtility.AttemptStartGame(state.QueuedZone);
                state.QueuedZone = null;
            }
        }
    }
}
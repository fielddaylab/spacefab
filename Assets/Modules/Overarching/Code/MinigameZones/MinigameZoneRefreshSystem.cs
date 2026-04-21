using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Clears MinigameZone single-frame interactions in Update, before PointerEvents get set
    /// </summary>
    public class MinigameZoneRefreshSystem : SystemComponent // ComponentSystemBehaviour<MinigameZone>
	{
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
			ecs.Register(&ProcessWork,
				new SysUpdate(GameLoopPhase.Update, 0),
				new SysPermissions().ReadWrite<MinigameZone>());
        }

        static private void ProcessWork(float deltaTime)
        {
            foreach (var zone in Find.Components<MinigameZone>())
            {
                zone.ClickedThisFrame = zone.PointerEnterThisFrame = zone.PointerExitThisFrame = false;
            }
        }
    }
}

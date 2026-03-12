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
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class MinigameZoneRefreshSystem : ComponentSystemBehaviour<MinigameZone>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            foreach (var zone in m_Components)
            {
                zone.ClickedThisFrame = zone.PointerEnterThisFrame = zone.PointerExitThisFrame = false;
            }
        }
    }
}

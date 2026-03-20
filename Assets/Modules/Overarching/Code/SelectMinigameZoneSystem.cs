using BeauUtil;
using FieldDay;
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
    [SysUpdate(GameLoopPhase.PreUpdate, 0)]
    public class SelectMinigameZoneSystem : ComponentSystemBehaviour<MinigameZone>
    {
        public override bool HasWork()
        {
            return base.HasWork();
        }


        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            MinigameZonesState state = Find.State<MinigameZonesState>();
            PaletteState palette = Find.State<PaletteState>();

            foreach (var zone in m_Components)
            {
                if (zone.PointerExitThisFrame)
                {
                    MinigameZonesUtility.CancelHover(state, zone.ZoneIndex);
                }
            }

            foreach (var zone in m_Components)
            {
                if (zone.PointerEnterThisFrame)
                {
                    MinigameZonesUtility.BeginHover(state, palette, zone.ZoneIndex);
                }
            }

            foreach (var zone in m_Components)
            {
                if (zone.ClickedThisFrame)
                {
                    MinigameZonesUtility.ClickZone(state, palette, zone.ZoneIndex);
                }
            }
        }
    }
}
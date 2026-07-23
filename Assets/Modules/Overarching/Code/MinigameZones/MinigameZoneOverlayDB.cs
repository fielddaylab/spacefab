using System;
using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// GlobalAsset mapping each MinigameId to its zone-overlay sprites. The colored overlay drawn
    /// over a minigame zone is looked up here by (MinigameId, focus) instead of being authored per
    /// MinigameZone, so the art lives in one place. Authored once as a project asset
    /// (CreateAssetMenu -> SpaceFab/Overarching/Minigame Zone Overlay DB) and fetched at runtime
    /// via Find.GlobalAsset by SelectMinigameZoneSystem.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Overarching/Minigame Zone Overlay DB")]
    public class MinigameZoneOverlayDB : GlobalAsset
    {
        [Serializable]
        public struct ZoneOverlay
        {
            public MinigameId Minigame;
            // Resting overlay shown over an unlocked, non-hovered zone.
            public Sprite NonFocus;
            // Swapped in while the zone is hovered.
            public Sprite Focus;
            // The minigame's representative color. Tints the alert icon's inner sprite, the station
            // label's dot (always), and the station label background while the zone is focused.
            public Color ZoneColor;
        }

        public ZoneOverlay[] Overlays;

        // Shared background color for an unfocused station label. A focused label uses that
        // minigame's own ZoneColor instead.
        public Color LabelBackgroundColor = Color.black;
    }

    /// <summary>
    /// Static lookup paired with MinigameZoneOverlayDB. Mirrors the AlertIconDB / AlertIconDBUtility
    /// pattern (data on the GlobalAsset, lookup in a static utility).
    /// </summary>
    public static class MinigameZoneOverlayDBUtility
    {
        // Returns the overlay sprite for the given minigame in the requested focus state, or null
        // if the DB has no entry for that minigame (caller should hide the overlay).
        public static Sprite LookupOverlaySprite(MinigameZoneOverlayDB db, MinigameId minigame, bool focus)
        {
            if (db == null || db.Overlays == null) { return null; }
            for (int i = 0; i < db.Overlays.Length; i++)
            {
                if (db.Overlays[i].Minigame == minigame)
                {
                    return focus ? db.Overlays[i].Focus : null;
                }
            }
            return null;
        }

        // Returns the minigame's representative ZoneColor (alert inner sprite, label dot, focused
        // label background). Falls back to white (the identity tint) when the DB is missing the
        // minigame or hasn't been authored.
        public static Color LookupZoneColor(MinigameZoneOverlayDB db, MinigameId minigame)
        {
            if (db == null || db.Overlays == null) { return Color.white; }
            for (int i = 0; i < db.Overlays.Length; i++)
            {
                if (db.Overlays[i].Minigame == minigame)
                {
                    return db.Overlays[i].ZoneColor;
                }
            }
            return Color.white;
        }
    }
}

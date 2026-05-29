using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Pooled worldspace icon overlay above a MinigameZone. One instance per set AlertType bit;
    /// allocated from OverarchingPools.AlertIconPool by OverarchingAlertSystem when AlertVisualsDirty
    /// is consumed, parented under MinigameZone.AlertIconContainer and positioned at a fixed
    /// horizontal offset based on its stack index.
    /// </summary>
    public class AlertIconView : BatchedComponent
    {
        // Three layered sprites, back to front: the base plate, the inner shape (tinted per-minigame
        // by OverarchingAlertSystem), and the white symbol on top. Sprites + inner color are set on
        // spawn from AlertIconDB / MinigameZoneOverlayDB.
        public SpriteRenderer BaseRenderer;
        public SpriteRenderer InnerRenderer;
        public SpriteRenderer SymbolRenderer;
    }
}

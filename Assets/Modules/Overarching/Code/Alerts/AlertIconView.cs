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
        public SpriteRenderer IconRenderer;
    }
}

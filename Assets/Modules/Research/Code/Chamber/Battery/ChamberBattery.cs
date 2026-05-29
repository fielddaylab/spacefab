using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Meter rig for the Battery chamber's voltage dial. Lives on a small
    /// prefab variant (SmallBatteryMeter.prefab / BigBatteryMeter.prefab)
    /// that ResearchTransitionSystem instantiates under the
    /// BatteryChamberState.BatteryContainer Transform at minigame setup.
    ///
    /// VoltageLevelSlots holds the per-cell SpriteRenderers in order
    /// (slot 0 = lowest cell). The array length defines the maximum
    /// voltage magnitude the player can dial in either polarity:
    /// VoltageUtility.Increase / Decrease / Flip clamp against it, and
    /// VoltageUtility.RefreshVisualState fills slots [0..magnitude-1]
    /// with ResearchVoltageConfig.VoltageSlotFilled and the rest with
    /// VoltageSlotEmpty.
    /// </summary>
    public class ChamberBattery : BatchedComponent {
        public SpriteRenderer[] VoltageLevelSlots;
    }
}

using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Discrete voltage steps the player can dial through with VoltageControl.
    /// Single global asset; the active step is identified by an index into
    /// Voltages.
    ///
    /// CenterIndex marks the 0V position (used by VoltageUtility.Flip to
    /// mirror around it). DefaultIndex is the index VoltageControl resets to
    /// when adjustability is locked.
    ///
    /// Magnitude is bounded by the active ChamberBattery prefab's
    /// VoltageLevelSlots.Length (small=2, big=5), not by Voltages.Length.
    /// Voltages must be authored long enough to cover the largest prefab's
    /// range (CenterIndex ± maxMagnitude).
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/Voltage Config")]
    public class ResearchVoltageConfig : GlobalAsset
    {
        public float[] Voltages;
        public int CenterIndex;
        public int DefaultIndex;

        [Header("Battery Meter Prefabs")]
        public GameObject SmallBatteryMeterPrefab;
        public GameObject BigBatteryMeterPrefab;

        [Header("Voltage Meter Sprites")]
        public Sprite VoltageSlotFilled;
        public Sprite VoltageSlotEmpty;
    }
}

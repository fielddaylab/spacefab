using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Discrete voltage steps the player can dial through with VoltageControl.
    /// Single global asset; the active step is identified by an index into
    /// Voltages, and the icon at the same index is shown on the chamber UI.
    ///
    /// CenterIndex marks the 0V position (used by VoltageUtility.Flip to
    /// mirror around it). DefaultIndex is the index VoltageControl resets to
    /// when adjustability is locked.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/Voltage Config")]
    public class ResearchVoltageConfig : GlobalAsset
    {
        public Sprite[] VoltageIcons;
        public float[] Voltages;
        public int CenterIndex;
        public int DefaultIndex;
    }
}

using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Discrete heat steps the player can dial through with HeatControl.
    /// Single global asset; the active step is identified by an index into
    /// Temperatures.
    ///
    /// Magnitude is bounded by Temperatures.Length.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/Heat Config")]
    public class ResearchHeatConfig : GlobalAsset
    {
        public float[] Temperatures;
        public int DefaultIndex;

        [Header("Heat Meter Sprites")]
        public Sprite[] HeatLevels;
    }
}

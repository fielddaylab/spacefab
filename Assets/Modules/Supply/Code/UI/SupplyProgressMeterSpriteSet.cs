using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Per-state overlay sprites + colors for the Supply mini progress meter, consumed by
    /// SupplyProgressMeterUtility when refreshing cells. Registered as a GlobalAsset so every
    /// meter cell pulls the same set via Find.GlobalAsset. Each cell's always-visible base
    /// background is authored on the prefab; these drive the state overlay only.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Supply/Progress Meter Sprite Set")]
    public class SupplyProgressMeterSpriteSet : GlobalAsset {
        [Header("Risk")]
        // Always-visible base, also the empty-cell look.
        public Sprite RiskBase;
        public Sprite RiskFilled;
        public Color RiskColor = Color.white;

        [Header("Cost")]
        public Sprite CostBase;
        // One bar sprite, tinted remaining (yellow) or spent (red).
        public Sprite CostBar;
        public Color CostRemainingColor = Color.yellow;
        public Color CostSpentColor = Color.red;

        [Header("Time")]
        public Sprite TimeBase;
        public Sprite TimeFilled;
        public Color TimeColor = Color.white;
    }
}

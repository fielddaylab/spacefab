using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Per-state sprites consumed by ProgressMeterUtility when refreshing the meter.
    /// Registered as a GlobalAsset so any meter instance pulls the same set via
    /// Find.GlobalAsset&lt;ProgressMeterSpriteSet&gt;(). EMPTY needs no sprite — the
    /// overlay Image is disabled instead.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/UI/Contract Meter Sprite Set")]
    public class ContractMeterSpriteSet : GlobalAsset {
        public Sprite TimeFilled;
        public Sprite TimeEmpty;
        public Sprite RevenueFilled;
        public Sprite RevenueEmpty;
    }
}

using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Per-state sprites consumed by ProgressMeterUtility when refreshing the meter.
    /// Registered as a GlobalAsset so any meter instance pulls the same set via
    /// Find.GlobalAsset&lt;ProgressMeterSpriteSet&gt;(). EMPTY needs no sprite — the
    /// overlay Image is disabled instead.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/UI/Progress Meter Sprite Set")]
    public class ProgressMeterSpriteSet : GlobalAsset {
        public Sprite CyclePending;
        public Sprite CycleFilled;
        public Sprite FundsPendingReceived;
        public Sprite FundsPendingSpent;
        public Sprite FundsFilled;
    }
}

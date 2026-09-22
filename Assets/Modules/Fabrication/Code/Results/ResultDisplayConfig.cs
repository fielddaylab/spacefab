using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/Result Display Config")]
    public class ResultDisplayConfig : GlobalAsset
    {
        public Color SuccessBackgroundColor;
        public Color FailureBackgroundColor;

        public Color SuccessHeaderColor;
        public Color FailureHeaderColor;

        public Color SuccessSectionColor;
        public Color FailureSectionColor;
        
        public Sprite StationRatingFilled;
        public Sprite StationRatingEmpty;
    }
}
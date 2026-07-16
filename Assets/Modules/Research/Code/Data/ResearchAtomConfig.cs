using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Research
{
    [CreateAssetMenu(menuName = "SpaceFab/Research/Atom Config")]
    public class ResearchAtomConfig : GlobalAsset
    {
        [Header("Atomic View Sprites")]
        public Sprite EmptySlotSprite;
        public Sprite FilledSlotSprite;

        public Color ActiveSlotColor;
        public Color DisabledSlotColor;
        public Color InvalidSlotColor;
    }
}

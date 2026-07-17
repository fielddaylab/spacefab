using FieldDay.Components;
using UnityEngine;
using TMPro;

namespace SpaceFab.Research
{
    public class MaterialAtom : BatchedComponent
    {
        public SpriteRenderer MaterialSprite;
        public SpriteRenderer[] ElectronSprites;
        public TMP_Text Label;
    }
}
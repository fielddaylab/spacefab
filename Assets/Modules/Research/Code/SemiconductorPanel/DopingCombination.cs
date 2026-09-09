using BeauUtil.UI;
using FieldDay.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Research
{
    public enum DopingSlotType {
        Semiconductor,
        PDopant,
        NDopant
    }

    [System.Serializable]
    public struct DopingSlot {
        public DopingSlotType Type;
        public GameObject Hidden;
        public Image Image;
        public TMP_Text Label;
        public ResearchObservationChip Chip;
    }

    public class DopingCombination : BatchedComponent
    {
        public RoundedRectGraphic Background;
        public DopingSlot[] Slots;
        public GameObject CheckMark;
    }
}
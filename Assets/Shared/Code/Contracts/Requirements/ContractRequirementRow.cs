using FieldDay.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab {
    public sealed class ContractRequirementRow : GuiWidget {

        public Graphic Background;
        public float[] Heights;
        public Graphic[] FaintLines;

        [Header("Rows")]
        public ContractRequirementSubRow DefaultRow;
        public ContractRequirementSubRow[] DopantRows;
    }
}
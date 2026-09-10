using FieldDay.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab {
    public sealed class ContractRequirementRow : GuiWidget {

        public Graphic Background;

        [Header("Rows")]
        public ContractRequirementSubRow DefaultRow;
        public ContractRequirementSubRow[] DopantRows;
    }
}
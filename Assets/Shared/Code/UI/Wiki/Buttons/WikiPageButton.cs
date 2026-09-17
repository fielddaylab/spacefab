using BeauUtil.UI;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public class WikiPageButton : GuiButton.Style {
        public Image Icon;
        public Graphic Background;
        public Graphic Outline;

        [Header("Material Page")]
        public GameObject MaterialLabelGroup;
        public TextMeshProUGUI MaterialLabel;
        public LayoutSizeGroup MaterialLabelSizer;

        public override void OnClick(GuiButton button) {
            
        }

        public override void UpdateInteractionState(GuiWidgetInteractableState state, GuiWidget source, GuiWidgetUpdateFlags flags) {
            
        }
    }
}
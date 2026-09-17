using BeauUtil.UI;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI
{
    /// <summary>
    /// Inspector reference to the icon on a pooled wiki tab button, kept separate from the
    /// button's own background Image. Written by WikiVisualsUtility.RefreshTabStrip.
    /// </summary>
    public class WikiTab : GuiButton.Style
    {
        public Image TabIcon;
        public Graphic Background;
        public Graphic Outline;

        public override void OnClick(GuiButton button) {
            
        }

        public override void UpdateInteractionState(GuiWidgetInteractableState state, GuiWidget source, GuiWidgetUpdateFlags flags) {
            
        }
    }
}
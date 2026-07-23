using BeauRoutine;
using FieldDay.UI;
using FieldDay.UI.Animation;
using FieldDay.UI.Widgets;
using UnityEngine;

namespace SpaceFab.UI {
    public sealed class HoverButtonStyle : GuiButton.Style {
        public RectTransform Shift;
        public float HoverYShift;
        public float DownYShift;
        public float DisabledYShift;

        public override void OnClick(GuiButton button) {
            PopAnim.Play(button.LayoutOffset, PopAnim.Default);
        }

        public override void UpdateInteractionState(GuiWidgetInteractableState state, GuiWidget source, GuiWidgetUpdateFlags flags) {
            float y = 0;

            switch(state) {
                case GuiWidgetInteractableState.Down: {
                    y = DownYShift;
                    break;
                }
                case GuiWidgetInteractableState.Hover: {
                    y = HoverYShift;
                    break;
                }
                case GuiWidgetInteractableState.Disabled: {
                    y = DisabledYShift;
                    break;
                }
            }

            Vector3 anchorPos = Shift.anchoredPosition3D;
            anchorPos.y = y;
            Shift.anchoredPosition = anchorPos;
        }
    }
}
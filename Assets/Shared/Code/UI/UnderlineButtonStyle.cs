using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using FieldDay.UI;
using FieldDay.UI.Animation;
using FieldDay.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public sealed class UnderlineButtonStyle : GuiButton.Style {
        public Graphic Outline;
        public Graphic Flash;

        public override void OnClick(GuiButton button) {
            FlashAnim.Play(Flash, Flash.color.WithAlpha(0.5f), FlashAnim.Quick);
            PopAnim.Play(button.LayoutOffset, PopAnim.Default);
        }

        public override void UpdateInteractionState(GuiWidgetInteractableState state, GuiWidget source, GuiWidgetUpdateFlags flags) {
            Outline.enabled = state == GuiWidgetInteractableState.Hover || state == GuiWidgetInteractableState.Down;
            source.LayoutOffset.Offset0 = new Vector2(0, state == GuiWidgetInteractableState.Down ? -2 : 0);
        }
    }
}
using BeauUtil.UI;
using UnityEngine;

namespace FieldDay.UI.Widgets {
    public sealed class GuiButton : GuiWidget {
        public PointerListener.PointerEvent OnClick {
            get { return CursorHint.onClick; }
        }

        protected override void UpdateState(GuiWidgetStateFlags state, GuiWidgetUpdateFlags flags = GuiWidgetUpdateFlags.Default) {
            CursorHint.enabled = (state & GuiWidgetStateFlags.PauseInteractions) == 0;
        }
    }
}
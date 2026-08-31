using BeauUtil;
using BeauUtil.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI.Widgets {
    public sealed class GuiButton : GuiWidget {
        public abstract class Style : MonoBehaviour, IGuiWidgetStyle, IGuiWidgetInteractiveStyle {
            public abstract void OnClick(GuiButton button);
            public abstract void UpdateInteractionState(GuiWidgetInteractableState state, GuiWidget source, GuiWidgetUpdateFlags flags);
            public virtual void UpdateState(GuiWidgetStateFlags state, GuiWidgetStateFlags changed, GuiWidget source, GuiWidgetUpdateFlags flags) { }
        }

        [SerializeField] private Style m_Style;

        [NonSerialized] private GuiWidgetInteractableState m_InteractableState;
        [NonSerialized] private bool m_WasClicked = false;

        public PointerListener.PointerEvent OnClick {
            get { return CursorHint.onClick; }
        }

        public bool ConsumeClick() {
            bool former = m_WasClicked;
            m_WasClicked = false;
            return former;
        }

        protected override void Awake() {
            base.Awake();

            if (m_Style) {
                AssignBaseStyle(m_Style);

                CursorHint.onPointerDown.Register(HandleCursorEvent);
                CursorHint.onPointerUp.Register(HandleCursorEvent);
                CursorHint.onPointerEnter.Register(HandleCursorEvent);
                CursorHint.onPointerExit.Register(HandleCursorEvent);
            }
            
            CursorHint.onClick.Register(HandleCursorClick);

            TryUpdateInteractableState(this, ref m_InteractableState, m_Style, GuiWidgetUpdateFlags.Initialization);
        }

        protected override void OnDisable() {
            m_WasClicked = false;
            base.OnDisable();
        }

        protected override void UpdateState(GuiWidgetStateFlags state, GuiWidgetStateFlags change, GuiWidgetUpdateFlags flags = GuiWidgetUpdateFlags.Default) {
            CursorHint.enabled = (state & GuiWidgetStateFlags.PauseInteractions) == 0;
            if (CanvasGraphic) {
                CanvasGraphic.raycastTarget = (state & GuiWidgetStateFlags.PauseInteractions) == 0;
            }

            TryUpdateInteractableState(this, ref m_InteractableState, m_Style);
        }

        static private readonly Action<PointerListener.EventData> HandleCursorClick = (data) => {
            CursorHint cursor = Unsafe.FastCast<CursorHint>(data.Source);
            GuiButton button = Unsafe.FastCast<GuiButton>(cursor.Owner);
            button.m_WasClicked = true;
            if (button.m_Style) {
                button.m_Style.OnClick(button);
            }
        };

        static private readonly Action<PointerListener.EventData> HandleCursorEvent = (data) => {
            CursorHint cursor = Unsafe.FastCast<CursorHint>(data.Source);
            GuiButton button = Unsafe.FastCast<GuiButton>(cursor.Owner);
            TryUpdateInteractableState(button, ref button.m_InteractableState, button.m_Style);
        };
    }
}
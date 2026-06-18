using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay.Components;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI.Widgets {
    [RequireComponent(typeof(CanvasGroup))]
    public class GuiCompoundWidget : GuiWidget {
        [NonSerialized] protected CanvasGroup m_CanvasGroup;

        protected override void Awake() {
            base.Awake();
            this.CacheComponent(ref m_CanvasGroup);
        }

        protected override void UpdateState(GuiWidgetStateFlags state, GuiWidgetUpdateFlags flags = GuiWidgetUpdateFlags.Default) {
            this.CacheComponent(ref m_CanvasGroup).blocksRaycasts = (state & GuiWidgetStateFlags.PauseInteractions) == 0;
        }
    }
}
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay.Components;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI.Widgets {
    public class GuiCompoundWidget : GuiWidget {
        [NonSerialized] protected CanvasGroup m_CanvasGroup;
    }
}
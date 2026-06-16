using System;
using UnityEngine;

namespace FieldDay.UI.Widgets {
    public interface IGuiWidgetStyle {
        void UpdateState(GuiWidgetStateFlags state, GuiWidgetUpdateFlags flags);
    }

    public interface IGuiWidgetDataStyle<TValue> {
        void Populate(in TValue data, GuiWidgetUpdateFlags flags);
    }

    public interface IGuiWidgetRangedDataStyle<TValue> {
        void SetRange(in GuiDataWidgetRange<TValue> range, GuiWidgetUpdateFlags flags);
    }

    public abstract class GuiWidgetStyle<TValue> : MonoBehaviour, IGuiWidgetDataStyle<TValue>, IGuiWidgetStyle {
        public abstract void Populate(in TValue data, GuiWidgetUpdateFlags flags);
        public abstract void UpdateState(GuiWidgetStateFlags state, GuiWidgetUpdateFlags flags);
    }

    [Flags]
    public enum GuiWidgetUpdateFlags : ushort {
        Default = 0,
        Force = 0x01,
        NoAnimation = 0x02,
        IsIncrease = 0x04,
        IsDecrease = 0x08
    }

    [Flags]
    public enum GuiWidgetStateFlags : ulong {
        Default = 0,
        PauseInteractions = 0x01,
        CanIncrease = 0x02,
        CanDecrease = 0x04,
        HideControls = 0x08,
        IsEmpty = 0x10,
        IsFull = 0x20,
    }
}
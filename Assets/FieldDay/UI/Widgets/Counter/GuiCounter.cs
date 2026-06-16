using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Scenes;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.UI.Widgets {
    public sealed class GuiCounter : GuiWidget, IGuiDataWidget<int> {
        public abstract class Style : GuiWidgetStyle<int>, IGuiWidgetRangedDataStyle<int> {
            public virtual void SetRange(in GuiDataWidgetRange<int> range, GuiWidgetUpdateFlags flags) { }
            public override void UpdateState(GuiWidgetStateFlags state, GuiWidgetUpdateFlags flags) { }
        }

        [SerializeField] private int m_StartingValue;
        [SerializeField] private int m_MaxValue;
        [SerializeField, Required] private Style m_Style;

        [NonSerialized] private int m_CurrentValue = -1;

        private void Awake() {
            if (!GameLoop.IsBooted()) {
                GameLoop.QueueOnBoot(Init);
            } else {
                Init();
            }
        }

        private void Init() {
            AssignBaseStyle(m_Style);
            if (m_CurrentValue < 0) {
                SetValue(m_StartingValue, GuiWidgetUpdateFlags.Force | GuiWidgetUpdateFlags.NoAnimation);
            }
        }

        public int MaxValue {
            get { return m_MaxValue; }
        }

        public int Value {
            get { return m_CurrentValue; }
        }

        public void SetValue(int value, GuiWidgetUpdateFlags flags = 0) {
            if ((flags & GuiWidgetUpdateFlags.Force) == 0 && value == m_CurrentValue) {
                return;
            }

            int lastValue = m_CurrentValue;
            value = Math.Clamp(value, 0, m_MaxValue);
            m_CurrentValue = value;

            if (value < lastValue) {
                flags |= GuiWidgetUpdateFlags.IsDecrease;
                flags &= ~GuiWidgetUpdateFlags.IsIncrease;
            } else if (value > lastValue) {
                flags |= GuiWidgetUpdateFlags.IsIncrease;
                flags &= ~GuiWidgetUpdateFlags.IsDecrease;
            }

            m_Style.Populate(value, flags);

            GuiWidgetStateFlags state = m_StateFlags;
            if (value > 0) {
                state = (state & ~GuiWidgetStateFlags.IsEmpty) | GuiWidgetStateFlags.CanDecrease;
            } else {
                state = (state | GuiWidgetStateFlags.IsEmpty) & ~GuiWidgetStateFlags.CanDecrease;
            }

            if (value < m_MaxValue) {
                state = (state & ~GuiWidgetStateFlags.IsFull) | GuiWidgetStateFlags.CanIncrease;
            } else {
                state = (state | GuiWidgetStateFlags.IsFull) & ~GuiWidgetStateFlags.CanIncrease;
            }

            UpdateState(state, flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetValue(GuiWidgetUpdateFlags flags = 0) {
            SetValue(m_StartingValue, flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Variant GetVariantValue() {
            return m_CurrentValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVariantValue(Variant value, GuiWidgetUpdateFlags flags = 0) {
            SetValue(value.AsInt(), flags);
        }
    }
}
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay.Components;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI.Widgets {
    [RequireComponent(typeof(RectTransform))]
    public abstract class GuiWidget : BatchedComponent {
        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private IGuiPanel m_Panel;
        [NonSerialized] private LayoutOffset m_LayoutOffset;

        [SerializeField] private SerializedHash32 m_Id;
        [SerializeField] private SerializedHash32 m_Class;
        [SerializeField] private SerializedHash32 m_Group;

        [NonSerialized] protected GuiWidgetStateFlags m_StateFlags;
        [NonSerialized] protected IGuiWidgetStyle m_BaseStyle;

        #region Identifiers

        /// <summary>
        /// The identifier for this widget.
        /// </summary>
        public StringHash32 Id {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Id; }
        }

        /// <summary>
        /// The class identifier for this widget.
        /// </summary>
        public StringHash32 Class {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Class; }
        }

        /// <summary>
        /// The group identifier for this widget.
        /// </summary>
        public StringHash32 Group {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Group; }
        }

        #endregion // Identifiers

        #region References

        /// <summary>
        /// The RectTransform.
        /// </summary>
        public RectTransform Rect {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ReferenceEquals(m_RectTransform, null) ? (m_RectTransform = (RectTransform)transform) : m_RectTransform; }
        }

        /// <summary>
        /// The closest parent GuiPanel.
        /// </summary>
        public IGuiPanel Panel {
            get { return ReferenceEquals(m_Panel, null) ? (m_Panel = GetComponentInParent<IGuiPanel>()) : m_Panel; }
        }

        /// <summary>
        /// Layout position helper.
        /// </summary>
        public LayoutOffset LayoutOffset {
            get { return ReferenceEquals(m_LayoutOffset, null) ? (m_LayoutOffset = GetComponent<LayoutOffset>()) : m_LayoutOffset; }
        }

        #endregion // References

        #region State

        /// <summary>
        /// Returns if the state of the widget contains all the given flags.
        /// </summary>
        public bool HasState(GuiWidgetStateFlags state) {
            return (m_StateFlags & state) == state;
        }

        /// <summary>
        /// Returns if the state of the widget contains any of the given flags.
        /// </summary>
        public bool HasAnyState(GuiWidgetStateFlags state) {
            return (m_StateFlags & state) != 0;
        }

        protected void UpdateState(GuiWidgetStateFlags state, GuiWidgetUpdateFlags flags = 0) {
            if (m_StateFlags != state) {
                m_StateFlags = state;
                m_BaseStyle.UpdateState(state, flags);
            }
        }

        #endregion // State
    
        protected void AssignBaseStyle(IGuiWidgetStyle style) {
            Assert.NotNullOrDestroyed(style);
            m_BaseStyle = style;
            style.UpdateState(m_StateFlags, GuiWidgetUpdateFlags.Force | GuiWidgetUpdateFlags.NoAnimation);
        }
    }

    /// <summary>
    /// Widget data range.
    /// </summary>
    public struct GuiDataWidgetRange<TValue> {
        public readonly TValue Min;
        public readonly TValue Max;

        public GuiDataWidgetRange(in TValue min, in TValue max) {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Indicates that a widget contains a value of some type.
    /// </summary>
    public interface IGuiDataWidget {
        Variant GetVariantValue();
        void SetVariantValue(Variant variant, GuiWidgetUpdateFlags flags = 0);
        void ResetValue(GuiWidgetUpdateFlags flags = 0);
    }

    /// <summary>
    /// Indicates that a widget contains a value of a specific type.
    /// </summary>
    public interface IGuiDataWidget<TValue> : IGuiDataWidget {
        TValue Value { get; }
        void SetValue(TValue value, GuiWidgetUpdateFlags flags = 0);
    }
}
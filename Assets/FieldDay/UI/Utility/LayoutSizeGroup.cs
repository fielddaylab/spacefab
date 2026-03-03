using BeauUtil;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI {
#if UNITY_EDITOR
    [ExecuteAlways]
#endif // UNITY_EDITOR
    public sealed class LayoutSizeGroup : MonoBehaviour, ILayoutElement {
        public enum SyncMode {
            Size,
            PreferredSize,
            PreferredSizeUpdateRoot,
        }
        
        [Required] public RectTransform Root;
        public SyncMode Mode;

        public Vector2 Padding;
        public Vector2 MinSize;
        [Required] public RectTransform[] Children;

        [NonSerialized] private Vector2 m_LastKnownSize;
        [NonSerialized] private Vector2 m_LastPaddedSize;

        /// <summary>
        /// Returns the last known size.
        /// </summary>
        public Vector2 LastSize {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_LastKnownSize; }
        }

        public void Sync() {
            Sync(Root, Mode, Padding);
        }

        public void Sync(RectTransform root, SyncMode mode, Vector2 padding) {
            if (!root) {
                return;
            }

            float width, height;
            switch (Mode) {
                case SyncMode.Size:
                default: {
                    Vector2 localSize = root.rect.size;
                    width = localSize.x;
                    height = localSize.y;
                    break;
                }
                case SyncMode.PreferredSize:
                case SyncMode.PreferredSizeUpdateRoot: {
                    width = LayoutUtility.GetPreferredWidth(root);
                    height = LayoutUtility.GetPreferredHeight(root);
                    break;
                }
            }

            SetSize(new Vector2(width, height));
        }

        public void SetSize(Vector2 size) {
            size.x = Mathf.Ceil(Math.Max(size.x, MinSize.x));
            size.y = Mathf.Ceil(Math.Max(size.y, MinSize.y));

            if (m_LastKnownSize != size) {
                m_LastKnownSize = size;

                if (Root && Mode == SyncMode.PreferredSizeUpdateRoot) {
                    Root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    Root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                }

                size.x = Mathf.Ceil(size.x + Padding.x);
                size.y = Mathf.Ceil(size.y + Padding.y);
                m_LastPaddedSize = size;

                foreach (var child in Children) {
                    if (!child) {
                        continue;
                    }

                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                }
            }
        }

        #region ILayoutElement

        float ILayoutElement.minWidth {
            get { return MinSize.x; }
        }

        float ILayoutElement.preferredWidth {
            get { return m_LastPaddedSize.x; }
        }

        float ILayoutElement.flexibleWidth {
            get { return 0; }
        }

        float ILayoutElement.minHeight {
            get { return MinSize.y; }
        }

        float ILayoutElement.preferredHeight {
            get { return m_LastPaddedSize.y; }
        }

        float ILayoutElement.flexibleHeight {
            get { return 0; }
        }

        int ILayoutElement.layoutPriority {
            get { return 100; }
        }
        void ILayoutElement.CalculateLayoutInputHorizontal() {
        }

        void ILayoutElement.CalculateLayoutInputVertical() {
        }

        #endregion // ILayoutElement

#if UNITY_EDITOR
        private void LateUpdate() {
            if (Application.IsPlaying(this) || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            m_LastKnownSize = default;
            Sync();
        }
#endif // UNITY_EDITOR
    }
}
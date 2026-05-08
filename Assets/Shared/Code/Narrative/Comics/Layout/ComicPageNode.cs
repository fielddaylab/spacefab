using BeauUtil;
using FieldDay.Debugging;
using FieldDay.Scenes;
using System;
using UnityEditor;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicPageNode : MonoBehaviour, IEditModeOnly {
        public Color32 BackgroundColor = new Color(0.11f, 0.1f, 0.1f, 1);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Color bgColor = BackgroundColor;
            Gizmos.color = bgColor.WithAlpha(0.5f);
            Vector3 size = new Vector3(500, 500, 0);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(new Vector3(0, 0, 24), size);
        }

#endif // UNITY_EDITOR
    }

    [Serializable]
    public struct PageData {
        public PackedPoint Position;
        public short PackedRotation;
        public ushort PackedColor;
        public OffsetLengthU16 Panels;
        public OffsetLengthU16 Cameras;
    }
}
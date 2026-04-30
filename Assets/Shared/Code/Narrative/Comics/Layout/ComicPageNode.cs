using BeauUtil;
using FieldDay.Scenes;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicPageNode : MonoBehaviour, IEditModeOnly {
        public Color32 BackgroundColor = new Color(0.11f, 0.1f, 0.1f, 1);
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
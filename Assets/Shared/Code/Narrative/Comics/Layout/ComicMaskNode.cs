using BeauUtil;
using FieldDay.Scenes;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicMaskNode : MonoBehaviour, IEditModeOnly {
        public Color32 Color;
        public Vector2 Offset0;
        public Vector2 Offset1;
        public Vector2 Offset2;
        public Vector2 Offset3;
    }

    [Serializable]
    public struct MaskData {
        public PackedPoint P0;
        public PackedPoint P1;
        public PackedPoint P2;
        public PackedPoint P3;
        public ushort PackedColor;
    }
}
using BeauUtil;
using FieldDay.Scenes;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicPanelNode : MonoBehaviour, IEditModeOnly {
    }

    [Serializable]
    public struct PanelData {
        public StringHash32 Id;
        public PackedPoint Position;
        public short PackedRotation;
        public OffsetLengthU16 Layers;
        public ushort MaskIndex;
    }

    [Serializable]
    public struct PackedPoint {
        public short PackedX;
        public short PackedY;
    }
}
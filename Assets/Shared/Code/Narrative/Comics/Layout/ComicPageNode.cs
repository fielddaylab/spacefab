using BeauUtil;
using FieldDay.Debugging;
using FieldDay.Scenes;
using System;
using UnityEditor;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicPageNode : MonoBehaviour, IEditModeOnly {
    }

    [Serializable]
    public struct PageData {
        public PackedPoint Position;
        public short PackedRotation;
        public OffsetLengthU16 Panels;
        public OffsetLengthU16 Cameras;
    }
}
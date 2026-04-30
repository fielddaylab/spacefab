using BeauUtil;
using FieldDay.Scenes;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicCameraNode : MonoBehaviour, IEditModeOnly {
        [Range(0.1f, 2048)]
        public float ClipHeight = 16;
    }

    [Serializable]
    public struct CameraData {
        public StringHash32 Id;
        public PackedPoint Position;
        public short PackedRotation;
        public short PackedClipHeight;
    }
}
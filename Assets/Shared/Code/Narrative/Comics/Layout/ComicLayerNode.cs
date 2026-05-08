using BeauUtil;
using FieldDay.Scenes;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicLayerNode : MonoBehaviour, IEditModeOnly {
        [NonSerialized] public ushort CachedIndex;

        public ComicLayerNode Sibling;
    }

    [Serializable]
    public struct LayerData {
        public StringHash32 Id;
        public PackedPoint Position;
        public short PackedRotation;
        public LayerFlags Flags;
        public ushort MeshIndex;
        public ushort TextureIndex;
        public short RenderOrder;
        public ushort SiblingLayerIndex;
    }

    [Flags]
    public enum LayerFlags : ushort {
        FullyOpaque = 0x01
    }
}
using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicRenderElement : BatchedComponent {
        [Serializable] public sealed class Pool : SerializablePool<ComicRenderElement> { }

        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;

        [NonSerialized] public StringHash32 Id;
        [NonSerialized] public ComicRenderElement Sibling;
        [NonSerialized] public float Visibility;
        [NonSerialized] public Routine Animation;

        [NonSerialized] public ushort TextureIndex = ComicTexture.NullTextureIndex;
        [NonSerialized] public Material BaseMaterial;
        [NonSerialized] public Material TempMaterial;
    }
}
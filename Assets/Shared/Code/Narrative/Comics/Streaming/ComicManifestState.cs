using System;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicManifestState : SharedStateComponent {
        [NonSerialized] public ComicManifestHeadersCache Headers;
        [NonSerialized] public UnsafeSpan<byte> RawManifest;
        [NonSerialized] public UnsafeSpan<byte> RawBlob;
    }

    public unsafe struct ComicManifestHeadersCache {
        public ushort PageCount;
        public ushort PanelCount;
        public ushort LayerCount;
        public ushort MeshCount;
        public ushort TextureCount;

        // TODO: headers for the remaining resource types
        public ComicMeshHeader* Meshes;
    }
}
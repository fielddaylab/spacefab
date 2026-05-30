using System;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Comic {
    [SharedStateInitOrder(1000)]
    public sealed class ComicDebugState : SharedStateComponent, IRegistrationCallbacks {
        public MeshRenderer Renderer;
        public MeshFilter Filter;

        [NonSerialized] public Material TextureMaterial;
        [NonSerialized] public int CurrentElementIndex = 0;
        [NonSerialized] public ushort CurrentMeshKey = ComicMesh.NullIndex;
        [NonSerialized] public Mode CurrentMode;

        void IRegistrationCallbacks.OnDeregister() {
            DestroyImmediate(TextureMaterial);
        }

        void IRegistrationCallbacks.OnRegister() {
            TextureMaterial = new Material(Find.State<ComicResourcePool>().BaseMaterial);
            Renderer.sharedMaterial = TextureMaterial;
        }

        public enum Mode {
            Hidden,
            Meshes,
            Masks
        }
    }
}
using System;
using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Rendering;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicDebugSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&Update, new SysUpdate(GameLoopPhase.DebugUpdate, 0),
                new SysPermissions()
                    .ReadWriteShared<ComicDebugState>()
                    .ReadShared<ComicResourcePool>());
        }

        static private void Update(float dt) {
            if (!ComicsUtility.Manifest) {
                return;
            }

            Find.State(out ComicDebugState debugState, out ComicResourcePool resources);

            if (DebugInput.IsPressed(KeyCode.M)) {
                debugState.CurrentMode = (ComicDebugState.Mode) (((int)debugState.CurrentMode + 1) % 3);
                UpdateSelection(debugState, debugState.CurrentElementIndex);
            } else if (DebugInput.IsPressed(KeyCode.K)) {
                UpdateSelection(debugState, debugState.CurrentElementIndex + 1);
            } else if (DebugInput.IsPressed(KeyCode.J)) {
                UpdateSelection(debugState, debugState.CurrentElementIndex - 1);
            }

            if (debugState.CurrentMeshKey != ComicMesh.NullIndex) {
                resources.ActiveMeshes.TryGetValue(debugState.CurrentMeshKey, out Mesh mesh);
                debugState.Filter.sharedMesh = mesh;
                debugState.Renderer.enabled = true;
            } else {
                debugState.Renderer.enabled = false;
                debugState.Filter.sharedMesh = null;
            }
        }

        static private void UpdateSelection(ComicDebugState debugState, int elementIndex) {
            int newIndex = debugState.CurrentElementIndex;
            ushort newKey = ComicMesh.NullIndex;
            switch (debugState.CurrentMode) {
                case ComicDebugState.Mode.Masks: {
                    if (ComicsUtility.Manifest.Masks.Length > 0) {
                        newIndex = MathUtils.SafeMod(elementIndex, ComicsUtility.Manifest.Masks.Length);
                        newKey = ComicsUtility.PackMeshId((ushort) newIndex, StreamedMeshType.Mask);
                    }
                    break;
                }
                case ComicDebugState.Mode.Meshes: {
                    if (ComicsUtility.Manifest.Layers.Length > 0) {
                        newIndex = MathUtils.SafeMod(elementIndex, ComicsUtility.Manifest.Layers.Length);
                        LayerData layer = ComicsUtility.Manifest.Layers[newIndex];
                        debugState.TextureMaterial.SetTexture(DefaultShaderProps.MainTex, ComicsUtility.Manifest.Textures[layer.TextureIndex]);
                        newKey = ComicsUtility.PackMeshId(layer.MeshIndex, StreamedMeshType.Layer);
                    }
                    break;
                }
            }

            debugState.CurrentElementIndex = newIndex;

            if (debugState.CurrentMeshKey != newKey) {
                debugState.CurrentMeshKey = newKey;
                if (newKey != ComicMesh.NullIndex) {
                    ComicsUtility.PreloadMesh(newKey);
                }
            }
        }
    }
}
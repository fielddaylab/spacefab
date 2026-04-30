using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicResourcePool : SharedStateComponent, IRegistrationCallbacks, IScenePreload {
        public const int BufferSizeMiB = 56;

        public ComicRenderElement.Pool ElementPool;
        public Material BaseMaterial;

        [NonSerialized] public IPool<Mesh> MeshPool;
        [NonSerialized] public IPool<Material> MaterialPool;

        [NonSerialized] public Dictionary<ushort, Mesh> ActiveMeshes;
        [NonSerialized] public RingBuffer<LiveComicTexture> ActiveTextures;

        [NonSerialized] public Unsafe.ArenaHandle Allocator;

        void IRegistrationCallbacks.OnDeregister() {
            foreach (var mesh in ActiveMeshes.Values) {
                MeshPool.Free(mesh);
            }
            ActiveMeshes.Clear();

            ElementPool.Dispose();

            while (ActiveTextures.TryPeekFront(out LiveComicTexture tex)) {
                Destroy(tex.Texture);
                Destroy(tex.Material);
            }

            MeshPool.Dispose();
            MaterialPool.Dispose();

            Allocator.Release();
        }

        void IRegistrationCallbacks.OnRegister() {
            ElementPool.Initialize();
            ElementPool.Config.RegisterOnFree((p, e) => ComicResourceUtility.OnRenderElementFreed(this, e));

            MeshPool = new DynamicPool<Mesh>(ElementPool.Capacity, Pool.DefaultConstructor<Mesh>());
            MeshPool.Config.RegisterOnFree((l, m) => m.Clear(true));
            MeshPool.Config.RegisterOnDestruct((p, m) => DestroyImmediate(m));

            MaterialPool = new DynamicPool<Material>(ElementPool.Capacity / 4, (p) => new Material(BaseMaterial));
            MaterialPool.Config.RegisterOnDestruct((p, m) => DestroyImmediate(m));

            ActiveTextures = new RingBuffer<LiveComicTexture>(16, RingBufferMode.Fixed);

            Allocator = Unsafe.CreateArena(Unsafe.MiB * BufferSizeMiB, "Comics", Unsafe.AllocatorFlags.Default);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ElementPool.Prewarm();
            MeshPool.Prewarm();
            MaterialPool.Prewarm();

            // TODO: Fix incremental prewarm in BeauPools
            //int prewarmCounter = 1;
            //while(ElementPool.Count < ElementPool.Capacity) {
            //    ElementPool.Prewarm(prewarmCounter++);
            //    yield return null;
            //}

            //prewarmCounter = 1;
            //while (MeshPool.Count < MeshPool.Capacity) {
            //    MeshPool.Prewarm(prewarmCounter++);
            //    yield return null;
            //}

            //prewarmCounter = 1;
            //while (MaterialPool.Count < MaterialPool.Capacity) {
            //    MaterialPool.Prewarm(prewarmCounter++);
            //    yield return null;
            //}
            return null;
        }
    }

    static public class ComicResourceUtility {
        static public void OnRenderElementFreed(ComicResourcePool resourcePool, ComicRenderElement element) {
            element.Animation.Stop();
            element.BaseMaterial = null;
            element.Sibling = null;
            element.MeshRenderer.sharedMaterial = null;
            element.MeshFilter.sharedMesh = null;
            element.Id = null;
            element.Visibility = 0;

            if (element.TempMaterial != null) {
                resourcePool.MaterialPool.Free(element.TempMaterial);
                element.TempMaterial = null;
            }

            if (element.TextureIndex != ComicTexture.NullTextureIndex) {
                ReleaseTextureReference(resourcePool, element.TextureIndex);
                element.TextureIndex = ComicTexture.NullTextureIndex;
            }
        }

        static public bool ReleaseTextureReference(ComicResourcePool resourcePool, ushort textureIndex) {
            for(int i = 0; i < resourcePool.ActiveTextures.Count; i++) {
                ref LiveComicTexture tex = ref resourcePool.ActiveTextures[i];
                if (tex.TextureIndex == textureIndex) {
                    Assert.True(tex.RefCount > 0, "Unbalanced texture ref/unref");
                    tex.RefCount--;
                    return true;
                }
            }

            return false;
        }
    }
}
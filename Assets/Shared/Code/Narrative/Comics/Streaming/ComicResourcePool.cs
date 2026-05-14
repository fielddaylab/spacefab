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
        public const int BufferSizeMiB = 8;

        public ComicRenderElement.Pool ElementPool;
        public TransformPool ParentPool;

        public Material BaseMaterial;

        [NonSerialized] public IPool<Mesh> MeshPool;
        [NonSerialized] public IPool<Material> MaterialPool;

        [NonSerialized] public Dictionary<ushort, Mesh> ActiveMeshes;

        [NonSerialized] public Unsafe.ArenaHandle Allocator;
        [NonSerialized] public MeshData16<ComicMeshVertex> MaskBuilder;
        [NonSerialized] public VertexLayout MeshVertexLayout;

        void IRegistrationCallbacks.OnDeregister() {
            foreach (var mesh in ActiveMeshes.Values) {
                MeshPool.Free(mesh);
            }
            ActiveMeshes.Clear();

            ElementPool.Dispose();
            ParentPool.Dispose();

            MeshPool.Dispose();
            MaterialPool.Dispose();

            MaskBuilder.Dispose();
            Allocator.Release();
        }

        void IRegistrationCallbacks.OnRegister() {
            ElementPool.Initialize();
            ElementPool.Config.RegisterOnFree((p, e) => ComicResourceUtility.OnRenderElementFreed(this, e));
            
            MeshVertexLayout = VertexUtility.GenerateLayout(typeof(ComicMeshVertex), 0);

            MeshPool = new FixedPool<Mesh>(256, (p) => {
                Mesh mesh = new Mesh();
                mesh.MarkDynamic();
                return mesh;
            });
            MeshPool.Config.RegisterOnFree((l, m) => m.Clear(true));
            MeshPool.Config.RegisterOnDestruct((p, m) => DestroyImmediate(m));

            MaterialPool = new FixedPool<Material>(64, (p) => new Material(BaseMaterial));
            MaterialPool.Config.RegisterOnDestruct((p, m) => DestroyImmediate(m));

            ParentPool.Initialize();

            Allocator = Unsafe.CreateArena(Unsafe.MiB * BufferSizeMiB, "Comics", Unsafe.AllocatorFlags.Default);
            MaskBuilder = new MeshData16<ComicMeshVertex>(4, 6, MeshTopology.Triangles, false);

            ActiveMeshes = new Dictionary<ushort, Mesh>(32);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ElementPool.Prewarm();
            MeshPool.Prewarm();
            MaterialPool.Prewarm();
            ParentPool.Prewarm();

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

    static public partial class ComicResourceUtility {
        static public void OnRenderElementFreed(ComicResourcePool resourcePool, ComicRenderElement element) {
            element.CoroutineAnimation.Stop();
            Game.Animation.CancelAnimation(ref element.LiteAnimation);
            element.BaseMaterial = null;
            element.Sibling = null;
            element.MeshRenderer.sharedMaterial = null;
            element.MeshFilter.sharedMesh = null;
            element.Id = null;
            element.Visibility = 0;

            if (element.MeshId != ComicMesh.NullIndex) {
                if (resourcePool.ActiveMeshes.Remove(element.MeshId, out Mesh mesh)) {
                    resourcePool.MeshPool.Free(mesh);
                }
                ComicsUtility.CancelMeshPreload(element.MeshId);
                element.MeshId = ComicMesh.NullIndex;
            }

            if (element.TempMaterial != null) {
                resourcePool.MaterialPool.Free(element.TempMaterial);
                element.TempMaterial = null;
            }
        }
    }
}
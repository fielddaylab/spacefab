using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Collections;
using FieldDay.Rendering;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    [LateInitializeOrder(-250)]
    public class ComicLayoutState : SharedStateComponent, ISceneLateInitialize
    {
        [NonSerialized] public Transform[] PageHierarchies;
        [NonSerialized] public BitSet64 AllocatedPageMask;

        [NonSerialized] public RingBuffer<LayoutSpawnRequest> SpawnBuffer;
        [NonSerialized] public UniqueIdAllocator16 SpawnIdAllocator;
        
        [NonSerialized] public UnsafeBitSet SpawnedLayersMask;
        [NonSerialized] public UnsafeBitSet SpawnedMasksMask;

        void ISceneLateInitialize.LateInitialize() {
            Find.State(out ComicResourcePool resourcePool);
            var manifest = ComicsUtility.Manifest;

            if (manifest != null) {
                int pageCount = manifest.Pages.Length;
                int layerCount = manifest.Layers.Length;
                int maskCount = manifest.Masks.Length;

                PageHierarchies = new Transform[pageCount];
                SpawnBuffer = new RingBuffer<LayoutSpawnRequest>(32, RingBufferMode.Fixed);
                SpawnIdAllocator = new UniqueIdAllocator16(32, false);
                SpawnedLayersMask = new UnsafeBitSet(resourcePool.Allocator.AllocSpan<uint>(UnsafeBitSet.Size(layerCount)));
                SpawnedMasksMask = maskCount > 0 ? new UnsafeBitSet(resourcePool.Allocator.AllocSpan<uint>(UnsafeBitSet.Size(maskCount))) : default;
            }
        }
    }

    public struct LayoutSpawnRequest {
        public UniqueId16 RequestId;
        public ushort LayerIndex;
        public LayoutSpawnAnimationType Animation;
        public bool IsMask;
    }

    public enum LayoutSpawnAnimationType : ushort {

    }

    static public partial class ComicResourceUtility {
        public const int MaxPages = 64;
        static public readonly StringHash32 MaskElementId = "_Mask";

        #region Hierarchies

        static public void AllocatePageHierarchy(int pageIndex) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layout);
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(pageIndex >= 0 && pageIndex < manifest.Pages.Length);

            if (!layout.AllocatedPageMask.IsSet(pageIndex)) {
                Log.Msg("[ComicsUtility] Spawning hierarchy for page {0}", pageIndex);
                PageData pageData = manifest.Pages[pageIndex];
                Transform root = SpawnPageTransform(resourcePool, pageData, pageIndex);
                layout.PageHierarchies[pageIndex] = root;
                var panelRange = pageData.Panels;
                for(int i = panelRange.Offset; i < panelRange.End; i++) {
                    SpawnPanelTransform(resourcePool, root, manifest.Panels[i], i - panelRange.Offset);
                }
                layout.AllocatedPageMask.Set(pageIndex);
            }
        }

        static private Transform SpawnPageTransform(ComicResourcePool resourcePool, in PageData pageData, int pageIndex) {
            Vector2 pos = ComicsUtility.UnpackPoint(pageData.Position);
            float rot = ComicsUtility.UnpackDegrees(pageData.PackedRotation);
            Transform root = resourcePool.ParentPool.Alloc(pos, Quaternion.Euler(0, 0, rot), false);
            if (Game.IsEditor) {
                root.gameObject.name = "Page " + pageIndex.ToStringLookup();
            }
            return root;
        }

        static private Transform SpawnPanelTransform(ComicResourcePool resourcePool, Transform parent, in PanelData panelData, int panelIndex) {
            Vector2 pos = ComicsUtility.UnpackPointPrecise(panelData.Position);
            float rot = ComicsUtility.UnpackDegrees(panelData.PackedRotation);
            Transform panel = resourcePool.ParentPool.Alloc(pos, Quaternion.Euler(0, 0, rot), parent, false);
            if (Game.IsEditor) {
                panel.gameObject.name = string.Format("Panel {0}: {1}", panelIndex.ToStringLookup(), panelData.Id.ToDebugString());
            }
            return panel;
        }

        static public void FreePageHierarchy(int pageIndex) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layout);
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(pageIndex >= 0 && pageIndex < manifest.Pages.Length);

            if (layout.AllocatedPageMask.IsSet(pageIndex)) {
                Log.Msg("[ComicsUtility] Freeing hierarchy for page {0}", pageIndex);
                Transform root = layout.PageHierarchies[pageIndex];
                layout.PageHierarchies[pageIndex] = null;
                for(int i = root.childCount; i-- > 0;) {
                    FreePanelHierarchy(resourcePool, root.GetChild(i));
                }
                resourcePool.ParentPool.Free(root);
                layout.AllocatedPageMask.Unset(pageIndex);
            }
        }

        static private void FreePanelHierarchy(ComicResourcePool resourcePool, Transform panel) {
            for(int i = panel.childCount; i-- > 0;) {
                Transform child = panel.GetChild(i);
                bool freed = Pool.TryFree(child);
                Assert.True(freed, "Child was not pooled! What happened?");
            }
            resourcePool.ParentPool.Free(panel);
        }

        static public Transform GetPanelTransform(ushort panelIndex) {
            Find.State(out ComicLayoutState layout);
            ushort pageIndex = ComicsUtility.GetPageIndexForPanel(panelIndex);
            Assert.True(layout.AllocatedPageMask.IsSet(pageIndex), "Page hierarchy for page {0} not spawned!");
            return layout.PageHierarchies[pageIndex].GetChild(panelIndex - ComicsUtility.Manifest.Pages[pageIndex].Panels.Offset);
        }

        #endregion // Hierarchies

        #region Masks

        static public ComicRenderElement SpawnMask(ushort maskIndex) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layout);
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(maskIndex != ushort.MaxValue && maskIndex < manifest.Masks.Length);

            ushort panelIndex = ComicsUtility.GetPanelIndexForMask(maskIndex);
            Transform parent = GetPanelTransform(panelIndex);

            if (layout.SpawnedMasksMask.IsSet(maskIndex)) {
                // TODO: return spawned mask
            }

            ushort meshId = ComicsUtility.PackMeshId(maskIndex, StreamedMeshType.Mask);

            MaskData maskData = manifest.Masks[maskIndex];
            ComicRenderElement renderElement = resourcePool.ElementPool.Alloc(default, default, parent, false);
            renderElement.Type = ComicRenderElementType.Mask;
            renderElement.Id = MaskElementId;
            renderElement.BaseMaterial = resourcePool.TextureMaterials[0];
            renderElement.MeshFilter.sharedMesh = resourcePool.ActiveMeshes[meshId];

            resourcePool.SharedPropertyBlock.SetColor(DefaultShaderProps.Color, ComicsUtility.UnpackColor565(maskData.PackedColor));
            renderElement.MeshRenderer.SetPropertyBlock(resourcePool.SharedPropertyBlock);

            return renderElement;
        }

        static public ComicRenderElement SpawnLayer(ushort layerIndex) {
            Find.State(out ComicResourcePool resourcePool, out ComicLayoutState layout);
            ComicSequenceManifest manifest = ComicsUtility.Manifest;
            Assert.NotNullOrDestroyed(manifest);
            Assert.True(layerIndex != ushort.MaxValue && layerIndex < manifest.Layers.Length);

            ushort panelIndex = ComicsUtility.GetPanelIndexForLayer(layerIndex);
            Transform parent = GetPanelTransform(layerIndex);

            if (layout.SpawnedLayersMask.IsSet(layerIndex)) {
                // TODO: return spawned layer
            }

            ushort meshId = ComicsUtility.PackMeshId(layerIndex, StreamedMeshType.Layer);

            LayerData layerData = manifest.Layers[layerIndex];
            ComicRenderElement renderElement = resourcePool.ElementPool.Alloc(ComicsUtility.UnpackPointPrecise(layerData.Position), Quaternion.Euler(0, 0, ComicsUtility.UnpackDegrees(layerData.PackedRotation)), parent, false);
            renderElement.Type = ComicRenderElementType.Layer;
            renderElement.Id = layerData.Id;
            renderElement.BaseMaterial = resourcePool.TextureMaterials[0];
            renderElement.MeshFilter.sharedMesh = resourcePool.ActiveMeshes[meshId];

            resourcePool.SharedPropertyBlock.SetColor(DefaultShaderProps.Color, Color.white);
            renderElement.MeshRenderer.SetPropertyBlock(resourcePool.SharedPropertyBlock);

            return renderElement;
        }

        #endregion // Masks
    }
}
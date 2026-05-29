using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using FieldDay.Scripting;
using Leaf;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    [CreateAssetMenu(menuName = "SpaceFab/Narrative/Comic Sequence")]
    public sealed class ComicSequenceManifest : AssetPackBase {
        [Header("-- GENERATED --")]
        public PageData[] Pages;
        public PanelData[] Panels;
        public LayerData[] Layers;
        public CameraData[] Cameras;
        public MaskData[] Masks;
        public ComicMeshHeader[] Meshes;
        [HideInInspector] public byte[] CompressedMeshData;
        public Texture2D[] Textures;

        [Header("Configuration")]
        public LeafAsset Script;
        public AssetPack AdditionalAssets;

        [NonSerialized] private UniqueId16 m_LeafHandle;
        [NonSerialized] private Unsafe.PinnedArrayHandle<byte> m_PinnedMeshData;

        private void OnDestroy() {
            m_PinnedMeshData.Dispose();
        }

        public unsafe UnsafeSpan<byte> MeshBuffer {
            get { return new UnsafeSpan<byte>(m_PinnedMeshData.Address, m_PinnedMeshData.Length); }
        }

        protected override void Mount(AssetMgr mgr) {
            if (AdditionalAssets) {
                mgr.LoadPackage(AdditionalAssets);
            }

            if (Script) {
                m_LeafHandle = ScriptDBUtility.Load(Script);
            }

            m_PinnedMeshData = Unsafe.PinArray(CompressedMeshData);

            Assert.True(ComicsUtility.Manifest == null, "Cannot have multiple comic sequences loaded at a time");
            ComicsUtility.Manifest = this;
        }

        protected override void Unmount(AssetMgr mgr) {
            if (AdditionalAssets) {
                mgr.UnloadPackage(AdditionalAssets);
            }

            if (m_LeafHandle) {
                ScriptDBUtility.Unload(m_LeafHandle);
                m_LeafHandle = default;
            }

            m_PinnedMeshData.Dispose();

            if (ComicsUtility.Manifest == this) {
                ComicsUtility.Manifest = null;
            }
        }
    }

    static public partial class ComicsUtility {
        static public ComicSequenceManifest Manifest { get; set; }

        static public ushort GetPanelIndexForName(StringHash32 name) {
            Assert.NotNullOrDestroyed(Manifest);

            var panels = Manifest.Panels;
            for (int i = 0, len = panels.Length; i < len; i++) {
                if (panels[i].Id == name) {
                    return (ushort)i;
                }
            }

            Assert.Fail("Panel '{0}' not found", name);
            return ushort.MaxValue;
        }

        static public ushort GetPanelIndexForName(StringHash32 name, int pageIndex) {
            if (pageIndex >= 0) {
                Assert.NotNullOrDestroyed(Manifest);

                OffsetLengthU16 pageRange = Manifest.Pages[pageIndex].Panels;
                var panels = Manifest.Panels;
                for (int i = pageRange.Offset, end = pageRange.End; i < end; i++) {
                    if (panels[i].Id == name) {
                        return (ushort)i;
                    }
                }

                Assert.Fail("Panel '{0}' not contained in page {1}", name, pageIndex);
                return ushort.MaxValue;
            }

            return GetPanelIndexForName(name);
        }

        static public ushort GetPanelIndexForLayer(ushort layerIndex) {
            Assert.NotNullOrDestroyed(Manifest);

            var panels = Manifest.Panels;
            for(int i = 0, len = panels.Length; i < len; i++) {
                if (panels[i].Layers.Contains(layerIndex)) {
                    return (ushort) i;
                }
            }

            Assert.Fail("Layer Index {0} not contained in any panel", layerIndex);
            return ushort.MaxValue;
        }

        static public ushort GetPanelIndexForLayer(ushort layerIndex, int pageIndex) {
            if (pageIndex >= 0) {
                Assert.NotNullOrDestroyed(Manifest);

                OffsetLengthU16 pageRange = Manifest.Pages[pageIndex].Panels;
                var panels = Manifest.Panels;
                for (int i = pageRange.Offset, end = pageRange.End; i < end; i++) {
                    if (panels[i].Layers.Contains(layerIndex)) {
                        return (ushort) i;
                    }
                }

                Assert.Fail("Layer Index {0} not contained in page {1}", layerIndex, pageIndex);
                return ushort.MaxValue;
            }

            return GetPanelIndexForLayer(layerIndex);
        }

        static public ushort GetPanelIndexForMask(ushort maskIndex) {
            Assert.NotNullOrDestroyed(Manifest);

            var panels = Manifest.Panels;
            for (int i = 0, len = panels.Length; i < len; i++) {
                if (panels[i].MaskIndex == maskIndex) {
                    return (ushort) i;
                }
            }

            Assert.Fail("Layer Index {0} not contained in any panel", maskIndex);
            return ushort.MaxValue;
        }

        static public ushort GetPanelIndexForMask(ushort maskIndex, int pageIndex) {
            if (pageIndex >= 0) {
                Assert.NotNullOrDestroyed(Manifest);

                OffsetLengthU16 pageRange = Manifest.Pages[pageIndex].Panels;
                var panels = Manifest.Panels;
                for (int i = pageRange.Offset, end = pageRange.End; i < end; i++) {
                    if (panels[i].MaskIndex == maskIndex) {
                        return (ushort) i;
                    }
                }

                Assert.Fail("Mask Index {0} not contained in page {1}", maskIndex, pageIndex);
                return ushort.MaxValue;
            }

            return GetPanelIndexForMask(maskIndex);
        }

        static public ushort GetPageIndexForPanel(ushort panelIndex) {
            Assert.NotNullOrDestroyed(Manifest);

            var pages = Manifest.Pages;
            for (int i = 0, len = pages.Length; i < len; i++) {
                if (pages[i].Panels.Contains(panelIndex)) {
                    return (ushort) i;
                }
            }

            Assert.Fail("Panel Index {0} not contained in any page", panelIndex);
            return ushort.MaxValue;
        }
    }
}
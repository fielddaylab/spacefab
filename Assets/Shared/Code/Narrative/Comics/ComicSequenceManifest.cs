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
    }
}
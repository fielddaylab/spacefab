using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using FieldDay.Scripting;
using Leaf;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicSequenceManifest : ScriptableObject, IAssetPackage {
        public PageData[] Pages;
        public PanelData[] Panels;
        public LayerData[] Layers;
        public CameraData[] Cameras;
        public MaskData[] Masks;
        public ComicMeshHeader[] Meshes;

        public byte[] CompressedMeshData;

        public LeafAsset Script;
        public AssetPack AdditionalAssets;

        [NonSerialized] private int m_RefCount;
        [NonSerialized] private UniqueId16 m_LeafHandle;

        #region IAssetPackage

        void IAssetPackage.Mount(AssetMgr mgr) {
            if (AdditionalAssets) {
                mgr.LoadPackage(AdditionalAssets);
            }

            if (Script) {
                m_LeafHandle = ScriptDBUtility.Load(Script);
            }
        }

        void IAssetPackage.Unmount(AssetMgr mgr) {
            if (AdditionalAssets) {
                mgr.UnloadPackage(AdditionalAssets);
            }

            if (m_LeafHandle) {
                ScriptDBUtility.Unload(m_LeafHandle);
                m_LeafHandle = default;
            }
        }

        bool IRefCountedAsset.AddRef() {
            return (m_RefCount++) == 0;
        }

        bool IRefCountedAsset.RemoveRef() {
            Assert.True(m_RefCount > 0, "Unbalanced IAssetPackage.AddRef/RemoveRef calls");
            return (m_RefCount--) == 1;
        }

        bool IRefCountedAsset.IsReferenced() {
            return m_RefCount > 0;
        }

        #endregion // IAssetPackage

#if UNITY_EDITOR
        bool IStreamingBundleRoot.GetExportParameters(out IStreamingBundleRoot.ExportData export) {
            export = default;
            return true;
        }
#endif // UNITY_EDITOR
    }
}
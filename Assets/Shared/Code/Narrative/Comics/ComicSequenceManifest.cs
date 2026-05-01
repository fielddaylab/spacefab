using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using FieldDay.Scripting;
using Leaf;
using System;
using UnityEngine;

namespace SpaceFab.Comic {
    [CreateAssetMenu(menuName = "SpaceFab/Narrative/Comic Sequence")]
    public sealed class ComicSequenceManifest : ContentPack {
        public PageData[] Pages;
        public PanelData[] Panels;
        public LayerData[] Layers;
        public CameraData[] Cameras;
        public MaskData[] Masks;
        public ComicMeshHeader[] Meshes;

        public byte[] CompressedMeshData;

        public LeafAsset Script;
        public AssetPack AdditionalAssets;

        [NonSerialized] private UniqueId16 m_LeafHandle;

        protected override void Mount(AssetMgr mgr) {
            if (AdditionalAssets) {
                mgr.LoadPackage(AdditionalAssets);
            }

            if (Script) {
                m_LeafHandle = ScriptDBUtility.Load(Script);
            }
        }

        protected override void Unmount(AssetMgr mgr) {
            if (AdditionalAssets) {
                mgr.UnloadPackage(AdditionalAssets);
            }

            if (m_LeafHandle) {
                ScriptDBUtility.Unload(m_LeafHandle);
                m_LeafHandle = default;
            }
        }
    }
}
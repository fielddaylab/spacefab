using System;
using UnityEngine;
using BeauUtil.Debugger;
using BeauUtil;


#if UNITY_EDITOR
using ScriptableBake;
using System.IO;
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Default asset package.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Asset Pack", order = -300)]
    public sealed class AssetPack : AssetPackBase {
        [SerializeField] private GlobalAsset[] m_GlobalAssets = Array.Empty<GlobalAsset>();
        [SerializeField] private NamedAsset[] m_NamedAssets = Array.Empty<NamedAsset>();
        [SerializeField] private LiteAssetGroup[] m_LiteAssets = Array.Empty<LiteAssetGroup>();

        #region IAssetPackage

        protected override void Mount(AssetMgr mgr) {
            foreach (var global in m_GlobalAssets) {
                mgr.Register(global);
            }

            foreach (var named in m_NamedAssets) {
                mgr.AddNamed(named.name, named);
            }

            foreach(var lite in m_LiteAssets) {
                lite.RegisterAssets(mgr);
            }
        }

        protected override void Unmount(AssetMgr mgr) {
            foreach(var global in m_GlobalAssets) {
                mgr.Deregister(global);
            }

            foreach(var named in m_NamedAssets) {
                mgr.RemoveNamed(named.name, named);
            }

            foreach (var lite in m_LiteAssets) {
                lite.DeregisterAssets(mgr);
            }
        }

        #endregion // IAssetPackage

#if UNITY_EDITOR

        protected internal override void EditorRebuild() {
            ReadFromEditorDirectory(this);
        }

        /// <summary>
        /// Refreshes all assets for the given pack from the pack's editor directory.
        /// </summary>
        static public void ReadFromEditorDirectory(AssetPack pack) {
            string myDir = Baking.GetAssetDirectory(pack);
            GlobalAsset[] global = Baking.FindAssets<GlobalAsset>(myDir);
            NamedAsset[] named = Baking.FindAssets<NamedAsset>(myDir);
            LiteAssetGroup[] lite = Baking.FindAssets<LiteAssetGroup>(myDir);

            Array.Sort(named, (a, b) => a.GetType().FullName.CompareTo(b.GetType().FullName));

            bool isChanged = false;
            if (!ArrayUtils.ContentEquals(pack.m_GlobalAssets, global)) {
                isChanged = true;
                pack.m_GlobalAssets = global;
            }
            if (!ArrayUtils.ContentEquals(pack.m_NamedAssets, named)) {
                isChanged = true;
                pack.m_NamedAssets = named;
            }
            if (!ArrayUtils.ContentEquals(pack.m_LiteAssets, lite)) {
                isChanged = true;
                pack.m_LiteAssets = lite;
            }

            if (isChanged) {
                Log.Msg("[AssetPack] Contents of pack '{0}' updated", pack.name);
                EditorUtility.SetDirty(pack);
            }
        }

#endif // UNITY_EDITOR
    }
}
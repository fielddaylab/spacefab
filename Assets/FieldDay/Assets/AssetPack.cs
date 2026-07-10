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
        public enum IncludeBehavior {
            IncludeSubfolders,
            DirectoryOnly,
            Manual
        }

        [SerializeField] private IncludeBehavior m_IncludeMode = IncludeBehavior.IncludeSubfolders;
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
            if (m_IncludeMode != IncludeBehavior.Manual) {
                ReadFromEditorDirectory(this);
            } else {
                CleanUpIncludes(this);
            }
        }

        /// <summary>
        /// Removes all null and duplicate assets from the pack.
        /// </summary>
        static public void CleanUpIncludes(AssetPack pack) {
            GlobalAsset[] global = AssetUtility.Editor.StripNullAndDuplicateReferences(pack.m_GlobalAssets);
            NamedAsset[] named = AssetUtility.Editor.StripNullAndDuplicateReferences(pack.m_NamedAssets);
            LiteAssetGroup[] lite = AssetUtility.Editor.StripNullAndDuplicateReferences(pack.m_LiteAssets);

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

        /// <summary>
        /// Refreshes all assets for the given pack from the pack's editor directory.
        /// </summary>
        static public void ReadFromEditorDirectory(AssetPack pack) {
            string myDir = Baking.GetAssetDirectory(pack);
            Baking.AssetDirectorySearchMode searchMode = pack.m_IncludeMode == IncludeBehavior.DirectoryOnly ? Baking.AssetDirectorySearchMode.FolderOnly : Baking.AssetDirectorySearchMode.IncludeSubfolders;

            GlobalAsset[] global = Baking.FindAssets<GlobalAsset>(searchMode, myDir);
            NamedAsset[] named = Baking.FindAssets<NamedAsset>(searchMode, myDir);
            LiteAssetGroup[] lite = Baking.FindAssets<LiteAssetGroup>(searchMode, myDir);

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
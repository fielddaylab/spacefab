using UnityEngine;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Pack of other asset packages.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Meta Pack", order = -299)]
    public sealed class MetaPack : AssetPackBase {
        [SerializeField] private AssetPackBase[] m_Packages;

        #region IAssetPackage

        protected override void Mount(AssetMgr mgr) {
            foreach (var package in m_Packages) {
                mgr.LoadPackage(package);
            }
        }

        protected override void Unmount(AssetMgr mgr) {
            foreach(var package in m_Packages) {
                mgr.UnloadPackage(package);
            }
        }

        #endregion // IAssetPackage

#if UNITY_EDITOR

        protected internal override void EditorRebuild() {
            RemoveDuplicateReferences(this);
        }

        static public void RemoveDuplicateReferences(MetaPack pack) {
            if (AssetUtility.Editor.StripNullAndDuplicateReferences(ref pack.m_Packages, pack)) {
                Log.Warn("[MetaPack] Contents of pack '{0}' updated to remove null and self references", pack.name);
                EditorUtility.SetDirty(pack);
            }
        }

#endif // UNITY_EDITOR
    }
}
using UnityEngine;
using BeauUtil.Debugger;
using ScriptableBake;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Streamed pack definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Streamed Pack", order = -298)]
    public sealed class StreamedPack : ScriptableObject {
        [SerializeField] private AssetPackBase[] m_Packages;

        #region IAssetPackage

        internal void Mount(AssetMgr mgr) {
            foreach (var package in m_Packages) {
                mgr.LoadPackage(package);
            }
        }

        internal void Unmount(AssetMgr mgr) {
            foreach (var package in m_Packages) {
                mgr.UnloadPackage(package);
            }
        }

        #endregion // IAssetPackage

#if UNITY_EDITOR

#endif // UNITY_EDITOR
    }
}
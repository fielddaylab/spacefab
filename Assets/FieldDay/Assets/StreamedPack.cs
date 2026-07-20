using UnityEngine;
using BeauUtil.Debugger;
using BeauUtil;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Streamed pack definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Streamed Pack", order = -298)]
    public sealed class StreamedPack : ScriptableObject, IAssetPackage {
        [SerializeField, Required] private AssetPackBase[] m_Packages;

        [NonSerialized] private int m_RefCount;

        #region IRefCountedAsset

        bool IRefCountedAsset.AddRef() {
            return (m_RefCount++) == 0;
        }

        bool IRefCountedAsset.RemoveRef() {
            Assert.True(m_RefCount > 0, "Unbalanced AssetPack.AddRef/RemoveRef calls");
            return (m_RefCount--) == 1;
        }

        bool IRefCountedAsset.IsReferenced() {
            return m_RefCount > 0;
        }

        #endregion // IRefCountedAsset

        #region Mount

        void IAssetPackage.Mount(AssetMgr mgr) {
            foreach(var package in m_Packages) {
                mgr.LoadPackage(package);
            }
        }

        void IAssetPackage.Unmount(AssetMgr mgr) {
            foreach (var package in m_Packages) {
                mgr.UnloadPackage(package);
            }
        }

        #endregion // Mount

#if UNITY_EDITOR
        internal void EditorRebuild() {
            if (AssetUtility.Editor.StripNullAndDuplicateReferences(ref m_Packages)) {
                Log.Warn("[StreamedPack] Contents of pack '{0}' updated to remove null references", name);
                EditorUtility.SetDirty(this);
            }

            foreach(var package in m_Packages) {
                package.RecursiveRebuild();
            }
        }
#endif // UNITY_EDITOR
    }

    public sealed class StreamedPackIdAttribute : AssetNameAttribute {
        public StreamedPackIdAttribute()
            : base(typeof(StreamedPack)) { }
    }
}
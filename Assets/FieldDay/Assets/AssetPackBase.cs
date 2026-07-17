using System;
using UnityEngine;
using BeauUtil.Debugger;
using ScriptableBake;
using BeauUtil;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Scriptable asset package.
    /// </summary>
    public abstract class AssetPackBase : ScriptableObject, IAssetPackage, IBaked {
        [NonSerialized] private uint m_RefCount;

        protected abstract void Mount(AssetMgr mgr);
        protected abstract void Unmount(AssetMgr mgr);

        #region IAssetPackage

        void IAssetPackage.Mount(AssetMgr mgr) {
            Mount(mgr);
        }

        void IAssetPackage.Unmount(AssetMgr mgr) {
            Unmount(mgr);
        }

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

        #endregion // IAssetPackage

#if UNITY_EDITOR

        int IBaked.Order { get { return 10000; } }

        protected internal virtual void EditorRebuild() { }

        protected internal virtual void RecursiveRebuild() {
            EditorRebuild();
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            EditorRebuild();
            return EditorUtility.IsDirty(this);
        }

#endif // UNITY_EDITOR
    }
}
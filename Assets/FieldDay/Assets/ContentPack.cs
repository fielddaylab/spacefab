using System;
using UnityEngine;
using BeauUtil.Debugger;
using BeauUtil;

namespace FieldDay.Assets {
    /// <summary>
    /// Scriptable asset package.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Asset Pack", order = -300)]
    public abstract class ContentPack : ScriptableObject, IAssetPackage {
        [NonSerialized] private int m_RefCount;

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
    }
}
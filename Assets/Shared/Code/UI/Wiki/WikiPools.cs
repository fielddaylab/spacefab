using BeauPools;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Pools for the dynamically-spawned wiki buttons, on the wiki prefab root alongside
    /// WikiContent. Both strips hold WikiButton, so one nested pool type serves both.
    ///
    /// Each pool's authored pool-root is where free instances park; its spawn-root is the strip
    /// the allocated ones live under. Freeing never destroys, so the DynamicButton subscriptions
    /// WikiButton wires in OnRegister survive reuse.
    ///
    /// Populated by WikiPoolUtility.RebuildStrips.
    /// </summary>
    public class WikiPools : BatchedComponent, IScenePreload {
        [Serializable] public sealed class WikiButtonPool : SerializablePool<WikiButton> { }

        [Header("Tab Strip")]
        public WikiButtonPool TabButtonPool;

        [Header("Page Thumb Strip")]
        public WikiButtonPool PageThumbPool;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            TabButtonPool.Prewarm();
            PageThumbPool.Prewarm();
            return null;
        }
    }
}

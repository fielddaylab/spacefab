using System.Collections.Generic;
using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Runtime pools for the dynamically-spawned wiki UI buttons. Lives on the wiki prefab
    /// root alongside WikiContent. Each pool has a prefab (authored once in the base wiki
    /// prefab) and two parent RectTransforms: ActiveParent for in-use instances, FreeParent
    /// for parked (inactive) instances.
    ///
    /// Populated by WikiPoolUtility.RebuildStrips — called once on level-load and again after
    /// WikiUtility.UnlockPage. Not driven per-frame.
    ///
    /// The lists store WikiButton components directly (not GameObjects) because every pooled
    /// instance will be indexed / configured by its WikiButton fields at acquire time.
    /// </summary>
    public class WikiPools : BatchedComponent {
        [Header("Tab Strip")]
        public WikiButton TabButtonPrefab;
        public RectTransform TabButtonActiveParent;
        public RectTransform TabButtonFreeParent;

        [Header("Page Thumb Strip")]
        public WikiButton PageThumbPrefab;
        public RectTransform PageThumbActiveParent;
        public RectTransform PageThumbFreeParent;

        // Runtime pool state. Not serialized — populated by WikiPoolUtility.
        [HideInInspector] public List<WikiButton> TabActive = new List<WikiButton>();
        [HideInInspector] public List<WikiButton> TabFree = new List<WikiButton>();
        [HideInInspector] public List<WikiButton> PageThumbActive = new List<WikiButton>();
        [HideInInspector] public List<WikiButton> PageThumbFree = new List<WikiButton>();
    }
}

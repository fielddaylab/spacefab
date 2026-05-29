using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Acquire/release + rebuild logic for WikiPools. Called after any authoring-level change
    /// in what tabs or page thumbnails exist (level-load, unlock mutation), not per-frame.
    ///
    /// Design: one pool per button kind (Tab, PageThumb). Each pool has a prefab the first
    /// acquire instantiates from; subsequent acquires pull from the free list. Release never
    /// destroys — it reparents to the free parent and deactivates, so pointer subscriptions
    /// on each WikiButton persist across reuse.
    /// </summary>
    public static class WikiPoolUtility {
        // Releases all currently-active tab + thumb instances to the free lists, then acquires
        // one tab per WikiContent.Tabs entry and one thumb per (tab, page) pair. Indices are
        // baked onto each acquired WikiButton (TabIndex, and PageIndex for thumbs) so click
        // routing + availability resolution Just Work.
        //
        // Does NOT set per-frame visual state (active-tab highlight, window clipping, arrow
        // interactability) — that's WikiVisualsUpdateSystem's job.
        public static void RebuildStrips(WikiContent content, WikiPools pools) {
            if (content == null || pools == null) { return; }

            ReleaseAll(pools.TabActive, pools.TabFree, pools.TabButtonFreeParent);
            ReleaseAll(pools.PageThumbActive, pools.PageThumbFree, pools.PageThumbFreeParent);

            if (content.Tabs == null) { return; }

            // Spawn one tab button per authored tab. TabIndex is its position in the array.
            for (int tabIndex = 0; tabIndex < content.Tabs.Length; tabIndex++) {
                WikiButton tabButton = Acquire(pools.TabButtonPrefab, pools.TabActive, pools.TabFree, pools.TabButtonActiveParent);
                if (tabButton == null) { continue; }
                tabButton.TabIndex = tabIndex;
                tabButton.PageIndex = -1;
            }

            // Spawn one thumb per (tab, page) pair across every authored tab. Thumbs for the
            // non-active tab are spawned too — the visuals system hides them by TabIndex
            // comparison, so no rebuild is needed on tab-switch.
            for (int tabIndex = 0; tabIndex < content.Tabs.Length; tabIndex++) {
                WikiTabData tab = content.Tabs[tabIndex];
                if (tab == null || tab.Pages == null) { continue; }
                for (int pageIndex = 0; pageIndex < tab.Pages.Length; pageIndex++) {
                    WikiButton thumb = Acquire(pools.PageThumbPrefab, pools.PageThumbActive, pools.PageThumbFree, pools.PageThumbActiveParent);
                    if (thumb == null) { continue; }
                        thumb.TabIndex = tabIndex;
                        thumb.PageIndex = pageIndex;
                }
            }
        }

        // Pulls a WikiButton off the free list, or instantiates a fresh one from prefab if the
        // free list is empty. Reparents it to activeParent, activates it, and records it in
        // activeList. Returns null if prefab is missing (logged for authoring-error visibility).
        private static WikiButton Acquire(WikiButton prefab, List<WikiButton> activeList, List<WikiButton> freeList, RectTransform activeParent) {
            if (prefab == null) {
                return null;
            }

            WikiButton instance;
            if (freeList.Count > 0) {
                int lastIndex = freeList.Count - 1;
                instance = freeList[lastIndex];
                freeList.RemoveAt(lastIndex);
            }
            else {
                instance = Object.Instantiate(prefab, activeParent);
            }

            instance.transform.SetParent(activeParent, false);
            instance.gameObject.SetActive(true);
            activeList.Add(instance);
            return instance;
        }

        // Walks an active list, reparents every instance to the free parent, deactivates it,
        // and moves it to the free list. Leaves the active list empty.
        private static void ReleaseAll(List<WikiButton> activeList, List<WikiButton> freeList, RectTransform freeParent) {
            for (int i = 0; i < activeList.Count; i++) {
                WikiButton instance = activeList[i];
                if (instance == null || !instance.Available) { continue; }
                instance.transform.SetParent(freeParent, false);
                instance.gameObject.SetActive(false);
                freeList.Add(instance);
            }
            activeList.Clear();
        }
    }
}

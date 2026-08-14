using BeauUtil.Debugger;

namespace SpaceFab.UI {
    /// <summary>
    /// Rebuild logic for WikiPools, run when the set of tabs or page thumbnails changes — level
    /// load and unlock mutations, not per-frame.
    ///
    /// Rebuilds are differential rather than destroy-and-respawn: each strip is resized to the
    /// count the content asks for, then every instance in range has its identity reassigned by
    /// position. A rebuild that doesn't change the counts touches no GameObject active states.
    ///
    /// Sets identity only. Lock state comes from WikiAvailabilityUtility.ApplyUnlocks immediately
    /// after, and visual state from WikiVisualsUtility.
    /// </summary>
    public static class WikiPoolUtility {
        // Syncs both strips — and the per-tab page memory they're indexed alongside — to the
        // authored content. Thumbs for non-active tabs are spawned too; WikiVisualsUtility hides
        // them by TabIndex, so tab-switching needs no rebuild.
        public static void RebuildStrips(WikiState wikiState, WikiContent content, WikiPools pools) {
            Assert.NotNullOrDestroyed(wikiState, "Missing WikiState");
            Assert.NotNullOrDestroyed(content, "Missing WikiContent");
            Assert.NotNullOrDestroyed(pools, "Missing WikiPools");
            Assert.True(content.Tabs != null, "WikiContent has no authored tabs");

            // Ahead of the strips, so the selection repair ApplyUnlocks may run against a
            // fully-locked tab restores that tab's remembered page rather than dropping it.
            WikiUtility.EnsureTabPageMemory(wikiState, content.Tabs.Length);

            SyncTabStrip(content, pools);
            SyncPageThumbStrip(content, pools);

            // Instances just changed identity — and possibly count — so both strips need restyling.
            WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.TabStrip | WikiVisualDirty.Paginator);
        }

        // One tab button per authored tab. TabIndex is its position in the array.
        private static void SyncTabStrip(WikiContent content, WikiPools pools) {
            int desired = content.Tabs.Length;
            Resize(pools.TabButtonPool, desired);

            var active = pools.TabButtonPool.ActiveObjects;
            for (int tabIndex = 0; tabIndex < desired; tabIndex++) {
                Configure(active[tabIndex], WikiButtonKind.Tab, tabIndex, -1);
            }
        }

        // One thumb per (tab, page) pair, flattened across every tab in tab order. For a given
        // content authoring, slot N always maps to the same pair, so the reassignment below is
        // stable across rebuilds.
        private static void SyncPageThumbStrip(WikiContent content, WikiPools pools) {
            int desired = 0;
            for (int tabIndex = 0; tabIndex < content.Tabs.Length; tabIndex++) {
                WikiTabData tab = content.Tabs[tabIndex];
                Assert.NotNullOrDestroyed(tab, "WikiContent.Tabs has a null entry at index {0}", tabIndex);
                Assert.True(tab.Pages != null, "Wiki tab '{0}' has no authored pages", tab.name);
                desired += tab.Pages.Length;
            }

            Resize(pools.PageThumbPool, desired);

            var active = pools.PageThumbPool.ActiveObjects;
            int slot = 0;
            for (int tabIndex = 0; tabIndex < content.Tabs.Length; tabIndex++) {
                WikiTabData tab = content.Tabs[tabIndex];
                for (int pageIndex = 0; pageIndex < tab.Pages.Length; pageIndex++) {
                    Configure(active[slot], WikiButtonKind.PageThumb, tabIndex, pageIndex);

                    // give page thumbnails tags so they can be highlighted in tutorial
                    if (active[slot].Kind == WikiButtonKind.PageThumb && active[slot].ElementTag != null) {
                        active[slot].ElementTag.SetId(WikiElementTagUtility.PageThumbId(tab.Pages[pageIndex].name));
                    }
                    slot++;
                }
            }
        }

        // Grows or shrinks a pool's allocated set to exactly `desired` instances.
        //
        // Surplus is always freed from the tail. SerializablePool removes from its active list by
        // swapping in the last element, so freeing anything else would reorder the survivors and
        // break the positional reassignment the callers rely on; freeing the tail is a plain
        // truncation.
        private static void Resize(WikiPools.WikiButtonPool pool, int desired) {
            var active = pool.ActiveObjects;

            while (active.Count > desired) {
                pool.Free(active[active.Count - 1]);
            }
            while (active.Count < desired) {
                pool.Alloc();
            }
        }

        // Reassigns a button's identity, applied uniformly whether the instance survived the
        // resize or was just allocated, so there's no "did this come from the pool?" branch.
        //
        // Click handlers are deliberately not rebound. WikiButton subscribes to its DynamicButton
        // in OnRegister, which BatchedComponent fires on enable — the pool's alloc/free already
        // cycles that, and a retained instance never lost its subscription, so rebinding would
        // double-register.
        //
        // Available resets to true; ApplyUnlocks assigns the real value on the very next call and
        // owns the matching gameObject.SetActive.
        private static void Configure(WikiButton button, WikiButtonKind kind, int tabIndex, int pageIndex) {
            Assert.NotNullOrDestroyed(button, "Wiki pool handed back a null WikiButton");

            button.Kind = kind;
            button.TabIndex = tabIndex;
            button.PageIndex = pageIndex;
            button.Available = true;
        }
    }
}

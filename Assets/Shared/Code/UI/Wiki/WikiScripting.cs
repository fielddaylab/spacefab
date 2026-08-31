using BeauUtil;
using FieldDay;
using Leaf.Runtime;
using SpaceFab;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Leaf-callable commands for the shared wiki UI. Thin wrappers over WikiUtility that resolve
    /// state once and no-op when called outside a scene that hosts the wiki.
    /// </summary>
    public static class WikiScripting {
        // Collapse the wiki to its icon. No-op when no wiki is present in the scene, or when it's
        // already collapsed / mid-transition (WikiUtility.Close handles the latter two).
        [LeafMember("CloseWiki")]
        public static void Leaf_CloseWiki() {
            if (!Game.SharedState.Has<WikiState>()) { return; }
            WikiUtility.Close(Find.State<WikiState>());
        }

        // Open (expanding if collapsed) the wiki to a specific tab + page by id. Each id may be the
        // asset name (e.g. "Materials_Tabs", "Diode") or the authored display title (e.g.
        // "Materials") — the resolver tries asset name first, then title. Case-sensitive. Unknown
        // ids are dropped by the resolver. No-op when no wiki is present.
        [LeafMember("OpenWikiTo")]
        public static void Leaf_OpenWikiTo(string tabId, string pageId) {
            if (!Game.SharedState.Has<WikiState>()) { return; }
            WikiUtility.OpenTo(new StringHash32(tabId), new StringHash32(pageId));
        }

        // Select a tab by id (does not expand the wiki — use OpenWikiTo for that). Id is the tab asset
        // name, case-sensitive. Drops the request if the id doesn't match an authored tab. No-op
        // when no wiki is present.
        [LeafMember("SetTabById")]
        public static void Leaf_SetTabById(string tabId) {
            if (!Game.SharedState.Has<WikiState>()) { return; }

            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }

            WikiState wikiState = Find.State<WikiState>();
            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            WikiUtility.SelectTabById(wikiState, contents[0], progressState, new StringHash32(tabId));
        }

        // Select a page by id within the active tab (does not expand the wiki — use OpenWikiTo for
        // that). Id is the page asset name, case-sensitive. Drops the request if the id isn't a
        // page in the active tab or the page is locked. No-op when no wiki is present.
        [LeafMember("SetPageById")]
        public static void Leaf_SetPageById(string pageId) {
            if (!Game.SharedState.Has<WikiState>()) { return; }

            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }

            WikiState wikiState = Find.State<WikiState>();
            PlayerProgressState progressState = Find.State<PlayerProgressState>();
            WikiUtility.SelectPageById(wikiState, contents[0], progressState, new StringHash32(pageId));
        }

        // Select a tab by id (does not expand the wiki — use OpenWikiTo for that). Id is the tab asset
        // name, case-sensitive. Drops the request if the id doesn't match an authored tab. No-op
        // when no wiki is present.
        [LeafMember("GetTabId")]
        public static StringHash32 Leaf_GetTabId() {
            if (!Game.SharedState.Has<WikiState>()) { return null; }

            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return null; }

            WikiState wikiState = Find.State<WikiState>();
            Debug.Log(contents[0].Tabs[wikiState.ActiveTabIndex].AssetId.ToDebugString());
            return contents[0].Tabs[wikiState.ActiveTabIndex].AssetId;
        }
    }
}

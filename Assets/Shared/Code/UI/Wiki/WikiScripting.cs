using FieldDay;
using Leaf.Runtime;

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
            WikiUtility.Close();
        }
    }
}

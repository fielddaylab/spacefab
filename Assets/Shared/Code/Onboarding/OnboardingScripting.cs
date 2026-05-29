using BeauUtil;
using FieldDay;
using Leaf.Runtime;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Leaf-callable entry points for the onboarding highlight system. Mirrors the
    /// ComicScripting shape — thin static wrappers that resolve the highlight state
    /// once and hand off to OnboardingHighlightUtility.
    /// </summary>
    public static class OnboardingScripting {
        // Summon a highlight on the ElementTag with id `id`. lockFocus=true also gates
        // input until the highlight (or all locked highlights) is released. margin defaults
        // to OnboardingHighlightState.DefaultMargin when left at -1.
        [LeafMember("HighlightElement")]
        public static void Leaf_HighlightElement(string id, bool lockFocus = false, float margin = -1f) {
            OnboardingHighlightState highlightState = Find.State<OnboardingHighlightState>();
            if (highlightState == null) { return; }
            OnboardingHighlightUtility.Show(highlightState, NormalizeId(id), lockFocus, margin);
        }

        // Release a single highlight by id. No-op if no highlight is active for that id.
        [LeafMember("ReleaseHighlight")]
        public static void Leaf_ReleaseHighlight(string id) {
            OnboardingHighlightState highlightState = Find.State<OnboardingHighlightState>();
            if (highlightState == null) { return; }
            OnboardingHighlightUtility.Release(highlightState, NormalizeId(id));
        }

        // Release every active highlight and clear all focus locks in one call.
        [LeafMember("ReleaseAllHighlights")]
        public static void Leaf_ReleaseAllHighlights() {
            OnboardingHighlightState highlightState = Find.State<OnboardingHighlightState>();
            if (highlightState == null) { return; }
            OnboardingHighlightUtility.ReleaseAll(highlightState);
        }

        // Lowercases a Leaf-supplied id before hashing so tutorial scripts can address elements
        // case-insensitively. Registered ids are all lowercase (tray/design tags lowercase before
        // hashing; inspector-authored tags use lowercase kebab-case), so the hash matches regardless
        // of how the script author cased the id. StringHash32 hashing is itself case-sensitive, so
        // the normalization has to happen on the raw string here, before it becomes a hash.
        private static StringHash32 NormalizeId(string id) {
            return string.IsNullOrEmpty(id) ? default : new StringHash32(id.ToLowerInvariant());
        }
    }
}

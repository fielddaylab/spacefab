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
        public static void Leaf_HighlightElement(StringHash32 id, bool lockFocus = false, float margin = -1f) {
            OnboardingHighlightState highlightState = Find.State<OnboardingHighlightState>();
            if (highlightState == null) { return; }
            OnboardingHighlightUtility.Show(highlightState, id, lockFocus, margin);
        }

        // Release a single highlight by id. No-op if no highlight is active for that id.
        [LeafMember("ReleaseHighlight")]
        public static void Leaf_ReleaseHighlight(StringHash32 id) {
            OnboardingHighlightState highlightState = Find.State<OnboardingHighlightState>();
            if (highlightState == null) { return; }
            OnboardingHighlightUtility.Release(highlightState, id);
        }

        // Release every active highlight and clear all focus locks in one call.
        [LeafMember("ReleaseAllHighlights")]
        public static void Leaf_ReleaseAllHighlights() {
            OnboardingHighlightState highlightState = Find.State<OnboardingHighlightState>();
            if (highlightState == null) { return; }
            OnboardingHighlightUtility.ReleaseAll(highlightState);
        }
    }
}

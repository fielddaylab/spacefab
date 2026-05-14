using FieldDay.Components;

namespace SpaceFab.UI {
    /// <summary>
    /// Root BatchedComponent of the per-minigame wiki prefab variant. Carries the ordered list
    /// of tabs this minigame exposes. One instance per scene. Consumed by WikiSelectSystem (to
    /// resolve TabId/PageId lookups for OpenTo calls) and WikiVisualsUpdateSystem (to render
    /// tab/page content).
    /// </summary>
    public class WikiContent : BatchedComponent {
        public WikiTabData[] Tabs;

        // Number of page thumbnails visible at once in the paginator strip. Scrolling moves
        // the strip by one slot at a time, keeping the selected page inside the window.
        // Authored per minigame — default matches the reference mockup.
        public int PageWindowSize = 5;
    }
}

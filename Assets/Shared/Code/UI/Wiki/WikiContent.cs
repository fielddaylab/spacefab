using System;
using FieldDay.Components;

namespace SpaceFab.UI {
    /// <summary>
    /// Root BatchedComponent of the shared wiki prefab. Carries the ordered list of tabs the
    /// active minigame exposes. One instance per scene. Consumed by WikiSelectSystem (to
    /// resolve TabId/PageId lookups for OpenTo calls) and WikiVisualsUtility (to render
    /// tab/page content).
    /// </summary>
    public class WikiContent : BatchedComponent {
        // Runtime-only: the tab set is authored per-minigame on GlobalUISceneConfig.WikiTabs and
        // pushed here by QuickToolbar on scene late enable. Empty until that happens, so a scene
        // without a config simply shows no tabs rather than tripping the strip asserts.
        [NonSerialized] public WikiTabData[] Tabs = Array.Empty<WikiTabData>();

        // Number of page thumbnails visible at once in the paginator strip. Scrolling moves the
        // strip one slot at a time, keeping the selected page inside the window.
        public int PageWindowSize = 5;
    }
}

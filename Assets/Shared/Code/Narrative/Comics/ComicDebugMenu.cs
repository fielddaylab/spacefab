using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;

namespace SpaceFab.Comic
{
    /// <summary>
    /// Development-only debug menu for the active comic. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot; compiled out of release builds since the attribute is
    /// Conditional). The skip button disables itself when no ComicLoader is in the active scene.
    /// </summary>
    public static class ComicDebugMenu
    {
        [DebugMenuFactory]
        private static DMInfo CreateComicDebugMenu()
        {
            DMInfo menu = new DMInfo("Comic", 1);
            menu.AddButton("Skip Comic", DebugSkipComic, HasActiveComicLoader);
            return menu;
        }

        // True only when a ComicLoader exists in the loaded scenes (i.e. a comic sequence is running).
        private static bool HasActiveComicLoader()
        {
            return FindActiveLoader() != null;
        }

        // Aborts the running comic and advances to the loader's NextScene.
        private static void DebugSkipComic()
        {
            ComicLoader loader = FindActiveLoader();
            if (loader == null)
            {
                Log.Warn("[ComicDebugMenu] Skip Comic unavailable: no active ComicLoader");
                return;
            }

            loader.Skip();
            Log.Msg("[ComicDebugMenu] Skipped comic '{0}'", loader.ComicId);
        }

        private static ComicLoader FindActiveLoader()
        {
            return Find.Any<ComicLoader>();
        }
    }
}

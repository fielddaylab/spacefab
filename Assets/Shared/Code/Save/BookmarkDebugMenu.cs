using System.Text;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;

namespace SpaceFab.Save {
    /// <summary>
    /// Development-only debug menu for bookmarks: contributes the Bookmarks submenu, the
    /// "Save as Bookmark" button, and the autosave toggle. Registered via [DebugMenuFactory]
    /// (auto-discovered at boot). Heading the menu "Save" merges these elements into the same root
    /// SaveUtility contributes, the way the three separate "Minigames" factories merge into one.
    ///
    /// NOTE: DMMenuUI builds its element views (PopulateMenu) BEFORE firing a menu's OnEnter, so
    /// mutating a DMInfo's Elements at runtime desyncs the views and crashes UpdateElements. The
    /// bookmark list is therefore built once at boot from the files on disk - a bookmark created
    /// mid-session appears in the list next play session, not immediately.
    /// </summary>
    public static class BookmarkDebugMenu {
        // Contributes the bookmark half of the Save root.
        [DebugMenuFactory]
        private static DMInfo CreateBookmarkDebugMenu() {
            DMInfo menu = new DMInfo("Save", 4);
            menu.AddText("Bookmark", AppendActiveBookmark);
            menu.AddToggle("Disable Autosave", IsAutosaveDisabled, SetAutosaveDisabled, CanToggleAutosave);
            menu.AddButton("Save as Bookmark", DebugCreateBookmark, CanCreateBookmark);
            menu.AddSubmenu(BuildBookmarkListMenu());
            return menu;
        }

        // One button per bookmark found at boot. An empty folder still gets a submenu holding a single
        // line, so it reads as empty rather than broken.
        private static DMInfo BuildBookmarkListMenu() {
            string[] names = BookmarkUtility.EnumerateNames();
            DMInfo menu = new DMInfo("Bookmarks", names.Length + 1);

            if (names.Length == 0) {
                menu.AddText("No bookmarks");
                return menu;
            }

            for (int i = 0; i < names.Length; i++) {
                string name = names[i]; // capture per-iteration so each button closes over its own name
                menu.AddButton(name, () => DebugLoadBookmark(name), CanLoadBookmark);
            }

            return menu;
        }

        // Live readout of the bookmark this session is running from, and the only signal explaining why
        // saving has gone quiet. Kept to one short line on purpose: DMInfo.SharedTextBuilder is
        // capacity-locked at 128 characters and throws rather than growing.
        private static void AppendActiveBookmark(StringBuilder sb) {
            if (!Game.SharedState.Has<SaveLoadState>()) {
                sb.Append("---");
                return;
            }

            string active = Find.State<SaveLoadState>().ActiveBookmark;
            sb.Append(string.IsNullOrEmpty(active) ? "---" : active);
        }

        private static bool IsAutosaveDisabled() {
            return Game.SharedState.Has<SaveLoadState>() && Find.State<SaveLoadState>().AutosaveDisabled;
        }

        private static void SetAutosaveDisabled(bool disabled) {
            if (!Game.SharedState.Has<SaveLoadState>()) {
                Log.Warn("[BookmarkDebugMenu] Autosave toggle unavailable: save state not present");
                return;
            }

            Find.State<SaveLoadState>().AutosaveDisabled = disabled;
        }

        // The toggle goes dead once a bookmark is active: saving is already blocked for good, and a
        // toggle that looked like it could turn saving back on would be a lie.
        private static bool CanToggleAutosave() {
            return Game.SharedState.Has<SaveLoadState>()
                && string.IsNullOrEmpty(Find.State<SaveLoadState>().ActiveBookmark);
        }

        // Creating a bookmark writes a project asset, so it is Editor-only. In builds the button stays
        // visible but permanently disabled, keeping the menu layout identical either way.
        private static bool CanCreateBookmark() {
#if UNITY_EDITOR
            // SaveMgr.Write reads the player code off UserSettingsState.
            return Game.SharedState.Has<SaveLoadState>() && Game.SharedState.Has<UserSettingsState>();
#else
            return false;
#endif // UNITY_EDITOR
        }

        private static void DebugCreateBookmark() {
#if UNITY_EDITOR
            if (!CanCreateBookmark()) {
                Log.Warn("[BookmarkDebugMenu] Save as Bookmark unavailable: required state not present");
                return;
            }

            BookmarkUtility.Create(Find.State<SaveLoadState>());
#endif // UNITY_EDITOR
        }

        private static bool CanLoadBookmark() {
            return Game.SharedState.Has<SaveLoadState>() && BookmarkUtility.CanLoad(Find.State<SaveLoadState>());
        }

        private static void DebugLoadBookmark(string name) {
            if (!CanLoadBookmark()) {
                Log.Warn("[BookmarkDebugMenu] Load Bookmark ignored: required state missing, or a save/load or scene load is in progress");
                return;
            }

            BookmarkUtility.Load(Find.State<SaveLoadState>(), name);
        }
    }
}

using BeauUtil;

namespace SpaceFab.UI {
    /// <summary>
    /// WikiPageData asset ids the wiki's own logic has to name directly,
    /// rather than reach through WikiContent.Tabs. Ids are page asset names —
    /// the same strings the chapter scripts pass to LockWikiPage /
    /// UnlockWikiPage, and the same ones PlayerProgressState.UnlockedWikiPages
    /// is keyed by — so they resolve in every scene, including ones whose tab
    /// set doesn't carry the page.
    /// </summary>
    public static class WikiConsts {
        // The introductory half of each pair of pages covering one
        // characteristic. Content keeps exactly one half of a pair unlocked:
        // chapter 1 locks the full pages ("Conductor" / "Insulator"), chapter 2
        // locks these two and restores them. Read by
        // WikiCharacteristicsLoadUtility to pick which half of the pair a
        // material page chips.
        public static readonly StringHash32 BasicConductorPageId = "Basic Conductor";
        public static readonly StringHash32 BasicInsulatorPageId = "Basic Insulator";
    }
}

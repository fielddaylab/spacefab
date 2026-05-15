using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Presentation state for the shared wiki UI. Holds expand/collapse status, the currently
    /// selected tab+page (last-viewed within the level session), one-frame external request
    /// flags set by WikiUtility.Open/Close/OpenTo, and the expand/collapse transition routine
    /// handle.
    ///
    /// Authoritative page/tab content lives on WikiContent + its referenced ScriptableObject
    /// assets; authoritative unlock set lives on PlayerProgressState.UnlockedWikiPages.
    /// </summary>
    public class WikiState : SharedStateComponent, IRegistrationCallbacks {
        // Steady-state: true when the full panel is visible; false when only the small icon
        // button is visible.
        [HideInInspector] public bool Expanded;

        // True while ExpandRoutine / CollapseRoutine is running. Read by Open/Close/OpenTo to
        // avoid firing a second transition on top of an in-flight one.
        [HideInInspector] public bool Transitioning;

        // Last-viewed selection within the current level session. Persists across expand /
        // collapse cycles; reset by the per-minigame transition system on level load.
        [HideInInspector] public int ActiveTabIndex;
        [HideInInspector] public int ActivePageIndex;

        // Paginator scroll offset. Indexes into the active tab's *unlocked* pages list — i.e.
        // the leftmost slot of the window shows the (PageWindowStartIndex)th unlocked page.
        // Maintained so ActivePageIndex's position-in-unlocked-list always falls within
        // [PageWindowStartIndex, PageWindowStartIndex + PageWindowSize).
        [HideInInspector] public int PageWindowStartIndex;

        // One-frame external request flags. Set by WikiUtility.Open / Close / OpenTo; consumed
        // and cleared inline by WikiSelectSystem during ProcessWork.
        [HideInInspector] public bool OpenRequestedThisFrame;
        [HideInInspector] public bool CloseRequestedThisFrame;
        [HideInInspector] public bool OpenToRequestedThisFrame;
        [HideInInspector] public StringHash32 RequestedTabId;
        [HideInInspector] public StringHash32 RequestedPageId;

        // Expand/collapse routine handle. Owned here so WikiUtility can Replace() it without
        // threading a MonoBehaviour owner through every call site.
        [HideInInspector] public Routine TransitionRoutine;
        
        [HideInInspector] public bool NeedsRebuild;

        public void OnRegister() {
            Expanded = false;
            Transitioning = false;
            ActiveTabIndex = 0;
            ActivePageIndex = 0;
            PageWindowStartIndex = 0;
            OpenRequestedThisFrame = false;
            CloseRequestedThisFrame = false;
            OpenToRequestedThisFrame = false;
            RequestedTabId = default;
            RequestedPageId = default;
        }

        public void OnDeregister() {
            TransitionRoutine.Stop();
        }
    }

    /// <summary>
    /// Main command surface for WikiState. Declared as partial so WikiButton.cs can extend it
    /// with the per-button pointer-event setters. Organized into regions: external API, tab
    /// and page selection, transition routines, unlock queries.
    /// </summary>
    public static partial class WikiUtility {
        #region External API

        // Open the wiki to the last-viewed tab + page. No-op if already expanded or mid-
        // transition. Callers: gameplay code, debug menus, the collapsed icon's click handler.
        public static void Open() {
            WikiState wikiState = Find.State<WikiState>();
            if (wikiState.Expanded || wikiState.Transitioning) { return; }
            wikiState.OpenRequestedThisFrame = true;
        }

        // Close the wiki. No-op if already collapsed or mid-transition.
        public static void Close() {
            WikiState wikiState = Find.State<WikiState>();
            if (!wikiState.Expanded || wikiState.Transitioning) { return; }
            wikiState.CloseRequestedThisFrame = true;
        }

        // Open the wiki to a specific tab + page. If expanded, selection changes without re-
        // running the expand transition. If collapsed, expands and then applies the selection.
        // Unknown IDs are dropped by the resolver (see SelectTabById / SelectPageById) — state
        // is left unchanged if either ID doesn't match an authored asset.
        public static void OpenTo(StringHash32 tabId, StringHash32 pageId) {
            WikiState wikiState = Find.State<WikiState>();
            if (wikiState.Transitioning) { return; }
            wikiState.OpenToRequestedThisFrame = true;
            wikiState.RequestedTabId = tabId;
            wikiState.RequestedPageId = pageId;
        }

        #endregion // External API

        #region Tab + Page Commands

        // Switch to a specific tab by index. Clamps to valid range. Resets ActivePageIndex to
        // the first unlocked page in the new tab (0 if none unlocked — a locked tab will show
        // no content anyway, and the availability utility should have hidden the button).
        // Also resets the paginator window to the leftmost position so tab-switching always
        // shows the start of the new tab's pages.
        public static void SelectTab(WikiState wikiState, WikiContent content, PlayerProgressState progressState, int tabIndex) {
            if (content.Tabs == null || content.Tabs.Length == 0) { return; }
            if (tabIndex < 0 || tabIndex >= content.Tabs.Length) { return; }

            wikiState.ActiveTabIndex = tabIndex;
            wikiState.ActivePageIndex = FirstUnlockedPageIndex(content.Tabs[tabIndex], progressState);
            wikiState.PageWindowStartIndex = 0;

            Debug.Log("Triggers rebuild: " + tabIndex);
            wikiState.NeedsRebuild = true;
        }

        // ID-based variant — resolves tabId → index via WikiContent.Tabs. Drops the request
        // silently if the ID doesn't match any authored tab.
        public static void SelectTabById(WikiState wikiState, WikiContent content, PlayerProgressState progressState, StringHash32 tabId) {
            int index = FindTabIndex(content, tabId);
            if (index < 0) { return; }
            SelectTab(wikiState, content, progressState, index);
        }

        // Advance to the next unlocked page in the active tab. Wraps to the first unlocked page
        // at the end. No-op if zero pages are unlocked.
        public static void NextPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            StepPage(wikiState, content, progressState, +1);
        }

        // Retreat to the previous unlocked page. Wraps to the last unlocked page at the start.
        public static void PrevPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            StepPage(wikiState, content, progressState, -1);
        }

        // Direct page selection by index. Clamps, then snaps to the nearest unlocked page at
        // or after the requested index (wrapping once). No-op if zero pages are unlocked.
        public static void SelectPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState, int pageIndex) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null || tab.Pages == null || tab.Pages.Length == 0) { return; }

            int clamped = Mathf.Clamp(pageIndex, 0, tab.Pages.Length - 1);
            int resolved = FindNextUnlockedFrom(tab, progressState, clamped, +1);
            if (resolved >= 0) {
                wikiState.ActivePageIndex = resolved;
                EnsureWindowContains(wikiState, content, tab, progressState);
            }

            Debug.Log("Triggers rebuild: " + pageIndex);
            wikiState.NeedsRebuild = true;
        }

        // ID-based variant. Drops the request if the ID doesn't match a page in the active tab.
        public static void SelectPageById(WikiState wikiState, WikiContent content, PlayerProgressState progressState, StringHash32 pageId) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null || tab.Pages == null) { return; }

            for (int i = 0; i < tab.Pages.Length; i++) {
                if (tab.Pages[i] != null && tab.Pages[i].AssetId == pageId) {
                    // Only snap to it if it's unlocked — keeps locked pages genuinely hidden.
                    if (IsPageUnlocked(progressState, pageId)) {
                        wikiState.ActivePageIndex = i;
                        EnsureWindowContains(wikiState, content, tab, progressState);
                    }
                    return;
                }
            }
        }

        #endregion // Tab + Page Commands

        #region Transition Routines

        // Expand from collapsed → full panel. Runs on WikiState.TransitionRoutine.
        // Scaffold body: flip the flags and yield one frame so visual systems get a chance to
        // observe Transitioning == true. A later visuals pass will layer in a CanvasGroup
        // alpha + scale tween via BeauRoutine.Tween, with this method's yield driving duration.
        public static IEnumerator ExpandRoutine(WikiState wikiState) {
            wikiState.Transitioning = true;
            wikiState.Expanded = true;
            yield return null;
            wikiState.Transitioning = false;
        }

        // Collapse from full panel → icon. Mirror of ExpandRoutine.
        public static IEnumerator CollapseRoutine(WikiState wikiState) {
            wikiState.Transitioning = true;
            wikiState.Expanded = false;
            yield return null;
            wikiState.Transitioning = false;
        }

        #endregion // Transition Routines

        #region Unlock Queries

        // True iff pageId appears in PlayerProgressState.UnlockedWikiPages.
        public static bool IsPageUnlocked(PlayerProgressState progressState, StringHash32 pageId) {
            if (progressState.UnlockedWikiPages == null) { return false; }
            return progressState.UnlockedWikiPages.Contains(pageId);

        }

        // True iff at least one page in the given tab is unlocked.
        public static bool IsTabUnlocked(PlayerProgressState progressState, WikiTabData tab) {
            if (tab == null || tab.Pages == null) { return false; }
            for (int i = 0; i < tab.Pages.Length; i++) {
                WikiPageData page = tab.Pages[i];
                if (page == null) { continue; }
                if (IsPageUnlocked(progressState, page.AssetId)) { return true; }
            }
            return false;
        }

        // Adds pageId to the unlocked set and dispatches GameEvents.WikiPageUnlocked so UI and
        // audio can react. Idempotent — duplicate unlocks don't re-dispatch.
        //
        // Caller (TODO): per-feature gameplay code calls this when the player first encounters
        // the concept (e.g., DesignTransitionSystem on the first level that introduces gates).
        // Caller is also responsible for invoking WikiAvailabilityUtility.ApplyUnlocks after so
        // the strip rebuilds to reveal the new tab / thumbnail.
        public static void UnlockPage(PlayerProgressState progressState, StringHash32 pageId) {
            progressState.UnlockedWikiPages ??= new System.Collections.Generic.HashSet<StringHash32>();
            if (!progressState.UnlockedWikiPages.Add(pageId)) { return; }

            SpacefabGame.Events.Dispatch(GameEvents.WikiPageUnlocked, pageId);
        }

        #endregion // Unlock Queries

        #region Internal Helpers

        // Resolves the currently-active tab asset, or null if the index is out of range /
        // content isn't authored yet.
        private static WikiTabData ActiveTab(WikiState wikiState, WikiContent content) {
            if (content.Tabs == null) { return null; }
            if (wikiState.ActiveTabIndex < 0 || wikiState.ActiveTabIndex >= content.Tabs.Length) { return null; }
            return content.Tabs[wikiState.ActiveTabIndex];
        }

        // Returns the index in content.Tabs whose asset name matches tabId, or -1 if not found.
        private static int FindTabIndex(WikiContent content, StringHash32 tabId) {
            if (content.Tabs == null) { return -1; }
            for (int i = 0; i < content.Tabs.Length; i++) {
                if (content.Tabs[i] != null && content.Tabs[i].AssetId == tabId) {
                    return i;
                }
            }
            return -1;
        }

        // Page-paginator step. Walks forward or backward by `dir` (±1), skipping locked pages,
        // wrapping at the ends. No-op if zero pages are unlocked in the active tab. After
        // updating ActivePageIndex, slides the paginator window by one slot in `dir` if the
        // new selection falls outside the current window (keeps the selected thumbnail visible).
        private static void StepPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState, int dir) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null || tab.Pages == null || tab.Pages.Length == 0) { return; }

            int startIndex = (wikiState.ActivePageIndex + dir + tab.Pages.Length) % tab.Pages.Length;
            int resolved = FindNextUnlockedFrom(tab, progressState, startIndex, dir);
            if (resolved >= 0) {
                wikiState.ActivePageIndex = resolved;
                EnsureWindowContains(wikiState, content, tab, progressState);
            }
        }

        // Scans the tab's pages starting at `from`, stepping by `dir`, wrapping once. Returns
        // the first unlocked page index found, or -1 if none exist.
        private static int FindNextUnlockedFrom(WikiTabData tab, PlayerProgressState progressState, int from, int dir) {
            int len = tab.Pages.Length;
            int i = from;
            for (int steps = 0; steps < len; steps++) {
                WikiPageData page = tab.Pages[i];
                if (page != null && IsPageUnlocked(progressState, page.AssetId)) {
                    return i;
                }
                i = (i + dir + len) % len;
            }
            return -1;
        }

        // Returns the index of the first unlocked page in `tab`, or 0 if none are unlocked
        // (caller is expected to hide fully-locked tabs via ApplyUnlocks).
        private static int FirstUnlockedPageIndex(WikiTabData tab, PlayerProgressState progressState) {
            if (tab == null || tab.Pages == null) { return 0; }
            int resolved = FindNextUnlockedFrom(tab, progressState, 0, +1);
            return resolved >= 0 ? resolved : 0;
        }

        // Counts unlocked pages in the given tab. Used to clamp the paginator window's right
        // edge and to answer "can we still scroll further right?".
        private static int UnlockedCount(WikiTabData tab, PlayerProgressState progressState) {
            if (tab == null || tab.Pages == null) { return 0; }
            int count = 0;
            for (int i = 0; i < tab.Pages.Length; i++) {
                WikiPageData page = tab.Pages[i];
                if (page != null && IsPageUnlocked(progressState, page.AssetId)) { count++; }
            }
            return count;
        }

        // Returns the position of rawIndex within the ordered list of unlocked pages in the
        // tab, or -1 if rawIndex itself is locked. Used to translate raw ActivePageIndex into
        // a "slot number in the paginator strip" for window-bound checks.
        private static int UnlockedIndexOf(WikiTabData tab, PlayerProgressState progressState, int rawIndex) {
            if (tab == null || tab.Pages == null) { return -1; }
            if (rawIndex < 0 || rawIndex >= tab.Pages.Length) { return -1; }
            WikiPageData target = tab.Pages[rawIndex];
            if (target == null || !IsPageUnlocked(progressState, target.AssetId)) { return -1; }

            int unlockedIndex = 0;
            for (int i = 0; i < rawIndex; i++) {
                WikiPageData page = tab.Pages[i];
                if (page != null && IsPageUnlocked(progressState, page.AssetId)) { unlockedIndex++; }
            }
            return unlockedIndex;
        }

        // Adjusts PageWindowStartIndex so the selected unlocked-page falls inside
        // [PageWindowStartIndex, PageWindowStartIndex + PageWindowSize). Shifts by the minimal
        // amount on either side. Also clamps the window so its right edge never passes the end
        // of the unlocked list (avoids a scrolled-past-the-end state after an unlock mutation
        // or window-size change).
        private static void EnsureWindowContains(WikiState wikiState, WikiContent content, WikiTabData tab, PlayerProgressState progressState) {
            int windowSize = content.PageWindowSize;
            if (windowSize <= 0) {
                wikiState.PageWindowStartIndex = 0;
                return;
            }

            int unlockedCount = UnlockedCount(tab, progressState);
            int selectedSlot = UnlockedIndexOf(tab, progressState, wikiState.ActivePageIndex);

            int start = wikiState.PageWindowStartIndex;

            // Slide window right if the selection is past its right edge.
            if (selectedSlot >= 0 && selectedSlot >= start + windowSize) {
                start = selectedSlot - windowSize + 1;
            }
            // Slide window left if the selection is before its left edge.
            if (selectedSlot >= 0 && selectedSlot < start) {
                start = selectedSlot;
            }

            // Clamp right edge: if the list is shorter than the window, start is always 0;
            // otherwise never let start exceed (unlockedCount - windowSize).
            int maxStart = Mathf.Max(0, unlockedCount - windowSize);
            start = Mathf.Clamp(start, 0, maxStart);

            wikiState.PageWindowStartIndex = start;
        }

        #endregion // Internal Helpers

        #region Paginator Queries

        // True iff the paginator window can still scroll one slot further left. Used by
        // WikiVisualsUpdateSystem to grey out the `<` arrow button at the leftmost state.
        public static bool CanScrollPageWindowLeft(WikiState wikiState) {
            return wikiState.PageWindowStartIndex > 0;
        }

        // True iff the paginator window can still scroll one slot further right. Used by
        // WikiVisualsUpdateSystem to grey out the `>` arrow button at the rightmost state.
        public static bool CanScrollPageWindowRight(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null) { return false; }
            int unlockedCount = UnlockedCount(tab, progressState);
            return wikiState.PageWindowStartIndex + content.PageWindowSize < unlockedCount;
        }

        // Translates a raw page index into its unlocked-list position (or -1 if the page is
        // locked). Exposed for WikiVisualsUpdateSystem: the visuals system walks the raw pages
        // list to render thumbnails and needs to know each thumbnail's slot for visibility
        // (slot in [PageWindowStartIndex, PageWindowStartIndex + PageWindowSize) is in-view).
        public static int GetUnlockedIndex(WikiTabData tab, PlayerProgressState progressState, int rawIndex) {
            return UnlockedIndexOf(tab, progressState, rawIndex);
        }

        #endregion // Paginator Queries
    }

    /// <summary>
    /// Rebuilds the dynamically-pooled tab + thumb button strips from WikiContent, then applies
    /// per-tab and per-page-thumbnail availability (locked vs. unlocked) to every WikiButton in
    /// the scene. Called on level-load and after any WikiUtility.UnlockPage mutation so buttons
    /// reflect the current content + unlock set.
    ///
    /// Rebuild comes first so the availability pass sees the newly-spawned instances (and any
    /// formerly-active instances that were released to the free pool are already hidden).
    ///
    /// For Tab buttons: Available = IsTabUnlocked(...). Locked tabs are hidden entirely —
    /// gameObject inactive, DynamicButton disabled — so no pointer events fire on them.
    /// For PageThumb buttons: Available iff the referenced page is unlocked. Visibility inside
    /// the paginator window and active-tab filtering are further runtime cuts handled by
    /// WikiVisualsUpdateSystem.
    /// For non-Tab/non-PageThumb buttons (arrows, exit, collapsed icon): Available stays true;
    /// they're chrome.
    ///
    /// Caller: TODO — the site that invokes this on level-load lives in each minigame's
    /// transition system; the scaffold does not yet wire it. WikiUtility.UnlockPage also needs
    /// to call this so mid-level unlocks reveal their tab / thumbnail.
    /// </summary>
    public static class WikiAvailabilityUtility {
        public static void ApplyUnlocks(WikiContent content, WikiPools pools, PlayerProgressState progressState) {
            WikiPoolUtility.RebuildStrips(content, pools);

            var buttons = Find.Components<WikiButton>();
            for (int i = 0; i < buttons.Count; i++) {
                WikiButton button = buttons[i];

                switch (button.Kind) {
                    case WikiButtonKind.Tab:
                        ApplyTabAvailability(button, content, progressState);
                        break;

                    case WikiButtonKind.PageThumb:
                        ApplyPageThumbAvailability(button, content, progressState);
                        break;

                    default:
                        // Chrome buttons are always available.
                        button.Available = true;
                        break;
                }
            }
        }

        private static void ApplyTabAvailability(WikiButton button, WikiContent content, PlayerProgressState progressState) {
            bool available = false;
            //bool available = true;
            if (content != null && content.Tabs != null
                && button.TabIndex >= 0 && button.TabIndex < content.Tabs.Length) {
                available = WikiUtility.IsTabUnlocked(progressState, content.Tabs[button.TabIndex]);
            }

            //button.Available = available;
            button.Available = true;
            button.gameObject.SetActive(available);
            if (button.DynamicButton != null) { button.DynamicButton.enabled = available; }
        }

        // Evaluates a PageThumb button's lock state against its *authoring* tab — i.e. the
        // tab this thumbnail was prefabbed under, inferred from the nearest parent Tab button.
        // For the scaffold, we simplify: a thumbnail is Available iff any tab it references
        // has its page unlocked. Prefab authoring is expected to place each thumbnail set
        // under the tab it belongs to; the visuals system then shows only the active tab's
        // thumbnails and hides the rest.
        //
        // Concretely: button.PageIndex is the raw index in content.Tabs[?].Pages. We resolve
        // "which tab?" by looking up the button's TabIndex field — prefab authoring is
        // expected to set TabIndex on thumbnails too, so thumbs carry both (TabIndex, PageIndex).
        private static void ApplyPageThumbAvailability(WikiButton button, WikiContent content, PlayerProgressState progressState) {
            bool available = false;
            if (content != null && content.Tabs != null
                && button.TabIndex >= 0 && button.TabIndex < content.Tabs.Length) {
                WikiTabData tab = content.Tabs[button.TabIndex];
                if (tab != null && tab.Pages != null
                    && button.PageIndex >= 0 && button.PageIndex < tab.Pages.Length) {
                    WikiPageData page = tab.Pages[button.PageIndex];
                    if (page != null) {
                        available = WikiUtility.IsPageUnlocked(progressState, page.AssetId);
                    }
                }
            }

            button.Available = available;
            button.gameObject.SetActive(available);
            if (button.DynamicButton != null) { button.DynamicButton.enabled = available; }
        }
    }
}

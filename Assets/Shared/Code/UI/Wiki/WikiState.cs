using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// The wiki's presentation domains, tracked separately so a mutation only repaints what it
    /// actually invalidated. Each WikiUtility mutator ORs in its domains at the point of mutation;
    /// WikiVisualsUtility.Refresh consumes and clears them.
    ///
    /// The paginator's scroll offset, thumbnails, highlight overlay and arrows share one bit on
    /// purpose: they read the same inputs, and the highlight is positioned from a RectTransform
    /// only the thumbnail pass can produce, so they can't be driven independently.
    /// </summary>
    [Flags]
    public enum WikiVisualDirty : byte {
        None = 0,

        // Expanded vs. collapsed root visibility.
        Visibility = 1 << 0,

        // Tab button icons, selected sprite, and width.
        TabStrip = 1 << 1,

        // Header title plus the active page's title / body / illustration / group toggles.
        PageContent = 1 << 2,

        // Paginator scroll offset, thumbnails, highlight overlay, and arrow enable-state.
        Paginator = 1 << 3,

        All = Visibility | TabStrip | PageContent | Paginator
    }

    /// <summary>
    /// Presentation state for the shared wiki UI: expand/collapse status, the selected tab and
    /// page, the paginator window offset, and the one-frame request flags external callers raise
    /// through WikiUtility.
    ///
    /// Tab and page content lives on WikiContent and its referenced assets; the unlock set lives
    /// on PlayerProgressState.UnlockedWikiPages.
    /// </summary>
    public class WikiState : SharedStateComponent, IRegistrationCallbacks {
        // True when the full panel is visible, false when only the collapsed icon is. Assigned
        // only by OnRegister, the two transition routines, and ForceCollapse.
        [HideInInspector] public bool Expanded;

        // True while a transition routine is in flight. Re-entrancy guard only, so a second
        // transition can't stack on an in-flight one — read by BeginExpand, BeginCollapse, and
        // OpenTo. Presentation reads Expanded instead.
        [HideInInspector] public bool Transitioning;

        // Last-viewed selection. Persists across expand/collapse cycles; reset on level load.
        [HideInInspector] public int ActiveTabIndex;
        [HideInInspector] public int ActivePageIndex;

        // Paginator scroll offset, counted in the active tab's *unlocked* pages: the window's
        // leftmost slot shows the (PageWindowStartIndex)th unlocked page. Kept such that the
        // selected page always falls inside [start, start + WikiContent.PageWindowSize).
        [HideInInspector] public int PageWindowStartIndex;

        // One-frame request flags. Raised by WikiUtility.Open / Close / OpenTo; consumed and
        // cleared inline by WikiSelectSystem.
        [HideInInspector] public bool OpenRequestedThisFrame;
        [HideInInspector] public bool CloseRequestedThisFrame;
        [HideInInspector] public bool OpenToRequestedThisFrame;
        [HideInInspector] public StringHash32 RequestedTabId;
        [HideInInspector] public StringHash32 RequestedPageId;

        // Expand/collapse routine handle. Owned here so WikiUtility can Replace() it without
        // threading a MonoBehaviour owner through every call site.
        [HideInInspector] public Routine TransitionRoutine;

        // Tab pop-out routine handle. Owned here for the same reason TransitionRoutine is, though the
        // routine itself is hosted on the scene's WikiLayoutState — the tab buttons it writes to die
        // with the wiki prefab, and this state doesn't.
        [HideInInspector] public Routine TabPopRoutine;

        // Tab the pop is settling on, or -1 before the first pop of a tab set. Matches
        // ActiveTabIndex in every steady state; a mismatch is the strip refresh's signal that the
        // selection has moved and the pop hasn't played yet, and names the tab to ease back in.
        [HideInInspector] public int PoppedTabIndex;

        // Requests a strip rebuild + unlock pass — the set of pooled button instances is wrong, as
        // opposed to VisualsDirty's "existing instances need restyling". Drained by
        // WikiRefreshSystem ahead of the visuals pass, and by OnSceneLateEnable on level load.
        [HideInInspector] public bool NeedsRebuild;

        // Which presentation domains are stale. Raised by the WikiUtility mutators at the point of
        // mutation, consumed and cleared by WikiVisualsUtility.Refresh. Unlike the *ThisFrame
        // flags this persists until drained, so an invalidation raised far from a refresh call
        // site — a mid-session UnlockPage, say — still lands.
        [HideInInspector] public WikiVisualDirty VisualsDirty;

        public void OnRegister() {
            Expanded = false;
            Transitioning = false;
            ActiveTabIndex = 0;
            ActivePageIndex = 0;
            PageWindowStartIndex = 0;
            PoppedTabIndex = -1;
            OpenRequestedThisFrame = false;
            CloseRequestedThisFrame = false;
            OpenToRequestedThisFrame = false;
            RequestedTabId = default;
            RequestedPageId = default;

            // Nothing has been painted yet.
            VisualsDirty = WikiVisualDirty.All;

            Game.Scenes.OnMainSceneLateEnable.Register(OnSceneLateEnable);
        }

        public void OnDeregister() {
            TransitionRoutine.Stop();
            TabPopRoutine.Stop();

            Game.Scenes.OnMainSceneLateEnable.Deregister(OnSceneLateEnable);
        }

        // Rebuilds the button strips and reapplies unlocks when the scene comes up, then styles
        // the resulting instances.
        public void OnSceneLateEnable()
        {
            if (!Game.SharedState.Has<WikiState>()
                || !Game.SharedState.Has<WikiLayoutState>()
                || !Game.SharedState.Has<WikiChipPools>()
                || !Game.SharedState.Has<PlayerProgressState>())
            {
                // not in a scene that needs wiki rebuilding
                return;
            }

            Find.State(
                out WikiState wikiState,
                out WikiLayoutState layoutState,
                out WikiChipPools chipPools,
                out PlayerProgressState progressState
            );

            if (!wikiState.NeedsRebuild) { return; }

            // No WikiContent means this scene doesn't ship the wiki prefab. Everything past this
            // point assumes the full authoring is present and asserts if it isn't.

            var pools = Find.Components<WikiPools>();
            Assert.True(pools.Count > 0, "WikiPools missing from a scene that has WikiContent");

            WikiPoolUtility.RebuildStrips(wikiState, layoutState.WikiContent, pools[0]);
            WikiAvailabilityUtility.ApplyUnlocks(layoutState.WikiContent, pools[0], progressState);
            wikiState.NeedsRebuild = false;

            // Painted here rather than left to WikiRefreshSystem's drain: waiting for the first
            // LateUpdate would show one frame of an unstyled panel on scene load.
            WikiVisualsUtility.Refresh(wikiState, layoutState, layoutState.WikiContent, pools[0], chipPools, progressState,
                WikiResearchContextUtility.Resolve());
        }
    }

    /// <summary>
    /// Command surface for WikiState. Partial so WikiButton.cs can extend it with the per-button
    /// pointer-event setters.
    /// </summary>
    public static partial class WikiUtility {
        #region External API

        // Open the wiki to the last-viewed tab + page. Raises the request unconditionally;
        // whether a transition actually starts is BeginExpand's call once WikiSelectSystem
        // consumes the flag, since state may change in between.
        public static void Open(WikiState wikiState) {
            wikiState.OpenRequestedThisFrame = true;
        }

        // Close the wiki. Mirror of Open — the guard lives in BeginCollapse.
        public static void Close(WikiState wikiState) {
            wikiState.CloseRequestedThisFrame = true;
        }

        // Open the wiki to a specific tab + page, expanding first if collapsed. Ids that match no
        // authored asset are dropped by SelectTabById / SelectPageById, leaving selection as-is.
        public static void OpenTo(StringHash32 tabId, StringHash32 pageId) {
            WikiState wikiState = Find.State<WikiState>();
            if (wikiState.Transitioning) { return; }
            wikiState.OpenToRequestedThisFrame = true;
            wikiState.RequestedTabId = tabId;
            wikiState.RequestedPageId = pageId;
        }

        // Toggle the wiki open or closed. Expanded only picks the direction; whether the chosen
        // transition runs is BeginExpand / BeginCollapse's call.
        public static void ToggleWikiOpen(WikiState wikiState)
        {
            if (wikiState.Expanded) {
                Close(wikiState);
            }
            else {
                Open(wikiState);
            }
        }

        // Pushes a scene's authored tab set into the wiki and resets the selection to the first
        // tab's first page — the previous scene's indices mean nothing against a new tab set.
        //
        // Raises NeedsRebuild rather than rebuilding here, so the load is order-independent with
        // respect to WikiState.OnSceneLateEnable: whichever of the two runs second does the work,
        // and WikiRefreshSystem's LateUpdate drain catches the case where neither did.
        public static void LoadTabs(WikiState wikiState, WikiContent content, WikiTabData[] tabs) {
            content.Tabs = tabs ?? Array.Empty<WikiTabData>();

            wikiState.ActiveTabIndex = 0;
            wikiState.ActivePageIndex = 0;
            wikiState.PageWindowStartIndex = 0;
            wikiState.NeedsRebuild = true;

            // The pooled tab buttons are about to be reassigned against a different tab set, so an
            // in-flight pop is animating the wrong instances. Drop it, and forget which tab was
            // popped — the index means nothing against a new tab set.
            wikiState.TabPopRoutine.Stop();
            wikiState.PoppedTabIndex = -1;

            // Every pooled instance is about to change identity, and the panel may already be
            // painted with the previous scene's content.
            WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.All);
        }

        #endregion // External API

        #region Material Page Lookup

        // Finds the page bound to materialId and returns the tab + page ids OpenTo needs. Material
        // pages can live under any tab, so every tab's page list is scanned. Returns false with
        // default out params when no page references the material.
        public static bool TryFindMaterialPage(WikiContent content, StringHash32 materialId, out StringHash32 tabId, out StringHash32 pageId) {
            tabId = default;
            pageId = default;
            if (content == null || content.Tabs == null || materialId.IsEmpty) { return false; }

            for (int t = 0; t < content.Tabs.Length; t++) {
                WikiTabData tab = content.Tabs[t];
                if (tab == null || tab.Pages == null) { continue; }
                for (int p = 0; p < tab.Pages.Length; p++) {
                    WikiPageData page = tab.Pages[p];
                    if (page != null && page.IsMaterialPage && page.MaterialId == materialId) {
                        tabId = tab.AssetId;
                        pageId = page.AssetId;
                        return true;
                    }
                }
            }
            return false;
        }

        // Finds the observation page covering observationType and returns the tab + page ids
        // OpenTo needs. Mirrors TryFindMaterialPage: observation pages can live under any tab, so
        // every tab's page list is scanned. Returns false with default out params when no page
        // covers the type.
        public static bool TryFindObservationPage(WikiContent content, ObservationType observationType, out StringHash32 tabId, out StringHash32 pageId) {
            tabId = default;
            pageId = default;
            if (content == null || content.Tabs == null) { return false; }

            for (int t = 0; t < content.Tabs.Length; t++) {
                WikiTabData tab = content.Tabs[t];
                if (tab == null || tab.Pages == null) { continue; }
                for (int p = 0; p < tab.Pages.Length; p++) {
                    WikiPageData page = tab.Pages[p];
                    if (page != null && page.IsObservationPage && page.ObservationType == observationType) {
                        tabId = tab.AssetId;
                        pageId = page.AssetId;
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion // Material Page Lookup

        #region Tab + Page Commands

        // Switch to a tab by index, selecting its first unlocked page and snapping the paginator
        // window back to the leftmost slot. Out-of-range indices are dropped.
        public static void SelectTab(WikiState wikiState, WikiContent content, PlayerProgressState progressState, int tabIndex) {
            if (content.Tabs == null || content.Tabs.Length == 0) { return; }
            if (tabIndex < 0 || tabIndex >= content.Tabs.Length) { return; }

            wikiState.ActiveTabIndex = tabIndex;
            wikiState.ActivePageIndex = FirstUnlockedPageIndex(content.Tabs[tabIndex], progressState);
            wikiState.PageWindowStartIndex = 0;
            wikiState.NeedsRebuild = true;

            // A tab switch moves the highlight, rebinds the page, and resets the window.
            WikiVisualsUtility.Invalidate(wikiState,
                WikiVisualDirty.TabStrip | WikiVisualDirty.PageContent | WikiVisualDirty.Paginator);
        }

        // Id-based variant. Matches the tab asset name first, then the display title, so callers
        // may pass either. Drops the request if neither matches.
        public static void SelectTabById(WikiState wikiState, WikiContent content, PlayerProgressState progressState, StringHash32 tabId) {
            int index = FindTabIndex(content, tabId);
            if (index < 0) { return; }
            SelectTab(wikiState, content, progressState, index);
        }

        // Advance to the next unlocked page in the active tab, wrapping at the end.
        public static void NextPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            StepPage(wikiState, content, progressState, +1);
        }

        // Step back to the previous unlocked page, wrapping at the start.
        public static void PrevPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            StepPage(wikiState, content, progressState, -1);
        }

        // Select a page by index, snapping forward to the nearest unlocked page (wrapping once).
        // The selection is left alone if the tab has no unlocked pages.
        public static void SelectPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState, int pageIndex) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null || tab.Pages == null || tab.Pages.Length == 0) { return; }

            int clamped = Mathf.Clamp(pageIndex, 0, tab.Pages.Length - 1);
            int resolved = FindNextUnlockedFrom(tab, progressState, clamped, +1);
            if (resolved >= 0) {
                wikiState.ActivePageIndex = resolved;
                EnsureWindowContains(wikiState, content, tab, progressState);
            }

            wikiState.NeedsRebuild = true;

            // A page change leaves the tab strip alone — only the bind and the strip below it.
            WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.PageContent | WikiVisualDirty.Paginator);
        }

        // Id-based variant, scoped to the active tab. Drops the request if nothing matches or the
        // matched page is locked.
        public static void SelectPageById(WikiState wikiState, WikiContent content, PlayerProgressState progressState, StringHash32 pageId) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null || tab.Pages == null) { return; }

            int index = FindPageIndexInTab(tab, pageId);
            if (index < 0) { return; }

            // Unlock is tracked by asset name, so test the resolved page's AssetId — pageId itself
            // may have been a display title.
            WikiPageData page = tab.Pages[index];
            if (!IsPageUnlocked(progressState, page.AssetId)) { return; }

            wikiState.ActivePageIndex = index;
            EnsureWindowContains(wikiState, content, tab, progressState);

            WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.PageContent | WikiVisualDirty.Paginator);
        }

        // Index of the page in tab matching pageId, or -1. Two passes mirroring FindTabIndex.
        private static int FindPageIndexInTab(WikiTabData tab, StringHash32 pageId) {
            // 1. Asset name.
            for (int i = 0; i < tab.Pages.Length; i++) {
                if (tab.Pages[i] != null && tab.Pages[i].AssetId == pageId) {
                    return i;
                }
            }

            // 2. Display title.
            for (int i = 0; i < tab.Pages.Length; i++) {
                WikiPageData page = tab.Pages[i];
                if (page != null && !string.IsNullOrEmpty(page.Title) && new StringHash32(page.Title) == pageId) {
                    return i;
                }
            }

            return -1;
        }

        #endregion // Tab + Page Commands

        #region Transition Routines

        // The single expand guard. Every path that wants the panel open goes through here rather
        // than testing Expanded / Transitioning itself.
        public static void BeginExpand(WikiState wikiState) {
            if (wikiState.Expanded || wikiState.Transitioning) { return; }

            // ExpandRoutine invalidates every domain, so the panel rebinds against fresh state
            // rather than whatever it was showing when it was last closed.
            wikiState.TransitionRoutine.Replace(ExpandRoutine(wikiState));
        }

        // The single collapse guard. Mirror of BeginExpand.
        public static void BeginCollapse(WikiState wikiState) {
            if (!wikiState.Expanded || wikiState.Transitioning) { return; }

            wikiState.TransitionRoutine.Replace(CollapseRoutine(wikiState));
        }

        // Hard reset for minigame teardown. Snaps straight to collapsed, skipping CollapseRoutine
        // and therefore the OnWikiClosed trigger — the scene is going away and nothing downstream
        // should react to it. Applies root visibility only, since the scene's WikiContent and
        // WikiPools are being torn down alongside this call.
        public static void ForceCollapse(WikiState wikiState, WikiLayoutState layoutState) {
            Assert.NotNullOrDestroyed(wikiState, "Missing WikiState");
            Assert.NotNullOrDestroyed(layoutState, "Missing WikiLayoutState");

            wikiState.TransitionRoutine.Stop();
            wikiState.TabPopRoutine.Stop();
            wikiState.Transitioning = false;
            wikiState.Expanded = false;
            wikiState.OpenRequestedThisFrame = false;
            wikiState.CloseRequestedThisFrame = false;
            wikiState.OpenToRequestedThisFrame = false;

            // Applied directly rather than through Invalidate: the scene is unloading, so there may
            // be no later drain. Clear the bit so the pending mask doesn't outlive the teardown.
            WikiLayoutUtility.ApplyExpandedSteadyState(layoutState, false);
            wikiState.VisualsDirty &= ~WikiVisualDirty.Visibility;
        }

        // Expand from collapsed to full panel. Runs on WikiState.TransitionRoutine; start it via
        // BeginExpand, never directly.
        public static IEnumerator ExpandRoutine(WikiState wikiState) {
            ScriptUtility.Trigger(ScriptTriggers.OnWikiOpened);

            wikiState.Transitioning = true;
            wikiState.Expanded = true;

            // Everything: the panel was hidden, so any domain invalidated while collapsed was left
            // pending, and content may have changed underneath it.
            WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.All);

            yield return null;
            wikiState.Transitioning = false;
        }

        // Collapse from full panel to icon. Mirror of ExpandRoutine; start it via BeginCollapse.
        public static IEnumerator CollapseRoutine(WikiState wikiState) {
            wikiState.Transitioning = true;
            wikiState.Expanded = false;

            // Only the roots change — the contents keep whatever they were last painted with.
            WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.Visibility);

            yield return null;
            wikiState.Transitioning = false;

            // Fired at the end rather than the start so a collapse interrupted by a reopen never
            // raises a spurious closed trigger.
            ScriptUtility.Trigger(ScriptTriggers.OnWikiClosed);
        }

        #endregion // Transition Routines

        #region Unlock Queries

        // True when pageId appears in PlayerProgressState.UnlockedWikiPages.
        public static bool IsPageUnlocked(PlayerProgressState progressState, StringHash32 pageId) {
            if (progressState == null || progressState.UnlockedWikiPages == null) { return false; }
            return progressState.UnlockedWikiPages.Contains(pageId);
        }

        // True when at least one page in the given tab is unlocked.
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
        // audio can react. Idempotent — a duplicate unlock doesn't re-dispatch.
        //
        // Raises NeedsRebuild so the new tab / thumbnail appears without the player having to
        // close and reopen the wiki.
        public static void UnlockPage(PlayerProgressState progressState, StringHash32 pageId) {
            if (!progressState.UnlockedWikiPages.Add(pageId)) { return; }

            WikiState wikiState = Find.State<WikiState>();
            if (wikiState != null) {
                wikiState.NeedsRebuild = true;

                // A newly-unlocked page can reveal its whole tab, and changes which thumbnails are
                // visible plus how far the window can scroll.
                WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.TabStrip | WikiVisualDirty.Paginator);
            }

            SpacefabGame.Events.Dispatch(GameEvents.WikiPageUnlocked, pageId);
        }

        #endregion // Unlock Queries

        #region Internal Helpers

        // The active tab asset, or null if the index is out of range or content isn't authored.
        private static WikiTabData ActiveTab(WikiState wikiState, WikiContent content) {
            if (content.Tabs == null) { return null; }
            if (wikiState.ActiveTabIndex < 0 || wikiState.ActiveTabIndex >= content.Tabs.Length) { return null; }
            return content.Tabs[wikiState.ActiveTabIndex];
        }

        // Index in content.Tabs matching tabId, or -1. Two passes, so an OpenTo caller can pass
        // either the asset file name ("Materials_Tabs") or the title shown in the UI ("Materials").
        private static int FindTabIndex(WikiContent content, StringHash32 tabId) {
            if (content.Tabs == null) { return -1; }

            // 1. Asset name.
            for (int i = 0; i < content.Tabs.Length; i++) {
                if (content.Tabs[i] != null && content.Tabs[i].AssetId == tabId) {
                    return i;
                }
            }

            // 2. Display title.
            for (int i = 0; i < content.Tabs.Length; i++) {
                WikiTabData tab = content.Tabs[i];
                if (tab != null && !string.IsNullOrEmpty(tab.Title) && new StringHash32(tab.Title) == tabId) {
                    return i;
                }
            }

            return -1;
        }

        // Steps the selection by `dir` (±1), skipping locked pages and wrapping at the ends, then
        // slides the paginator window so the new selection stays visible.
        private static void StepPage(WikiState wikiState, WikiContent content, PlayerProgressState progressState, int dir) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null || tab.Pages == null || tab.Pages.Length == 0) { return; }

            int startIndex = (wikiState.ActivePageIndex + dir + tab.Pages.Length) % tab.Pages.Length;
            int resolved = FindNextUnlockedFrom(tab, progressState, startIndex, dir);
            if (resolved >= 0) {
                wikiState.ActivePageIndex = resolved;
                EnsureWindowContains(wikiState, content, tab, progressState);

                WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.PageContent | WikiVisualDirty.Paginator);
            }
        }

        // Scans the tab's pages from `from`, stepping by `dir` and wrapping once. Returns the
        // first unlocked page index found, or -1 if none exist.
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

        // Index of the first unlocked page in `tab`, or 0 if none are — a fully-locked tab has its
        // button hidden by ApplyUnlocks, so the index never gets used.
        private static int FirstUnlockedPageIndex(WikiTabData tab, PlayerProgressState progressState) {
            if (tab == null || tab.Pages == null) { return 0; }
            int resolved = FindNextUnlockedFrom(tab, progressState, 0, +1);
            return resolved >= 0 ? resolved : 0;
        }

        // Number of unlocked pages in the tab — the paginator window's right bound.
        private static int UnlockedCount(WikiTabData tab, PlayerProgressState progressState) {
            if (tab == null || tab.Pages == null) { return 0; }
            int count = 0;
            for (int i = 0; i < tab.Pages.Length; i++) {
                WikiPageData page = tab.Pages[i];
                if (page != null && IsPageUnlocked(progressState, page.AssetId)) { count++; }
            }
            return count;
        }

        // Position of rawIndex within the tab's unlocked pages, or -1 if that page is locked.
        // Converts a raw page index into the paginator slot the window bounds are measured in.
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

        // Shifts PageWindowStartIndex by the minimum needed to bring the selected page inside the
        // window, then clamps so the window's right edge can't pass the end of the unlocked list.
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

            // A list shorter than the window pins start at 0.
            int maxStart = Mathf.Max(0, unlockedCount - windowSize);
            start = Mathf.Clamp(start, 0, maxStart);

            wikiState.PageWindowStartIndex = start;
        }

        #endregion // Internal Helpers

        #region Paginator Queries

        // True while the paginator window can still scroll left. Greys out the `<` arrow.
        public static bool CanScrollPageWindowLeft(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            WikiTabData tab = ActiveTab(wikiState, content);
            return tab != null && wikiState.PageWindowStartIndex > 0;
        }

        // True while the window's right edge hasn't reached the end of the unlocked list. Greys
        // out the `>` arrow.
        public static bool CanScrollPageWindowRight(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            WikiTabData tab = ActiveTab(wikiState, content);
            if (tab == null) { return false; }

            int windowSize = Mathf.Max(1, content.PageWindowSize);
            return wikiState.PageWindowStartIndex + windowSize < UnlockedCount(tab, progressState);
        }

        // Public form of UnlockedIndexOf. WikiVisualsUtility walks the raw page list to style
        // thumbnails and needs each one's slot to test it against the window bounds.
        public static int GetUnlockedIndex(WikiTabData tab, PlayerProgressState progressState, int rawIndex) {
            return UnlockedIndexOf(tab, progressState, rawIndex);
        }

        #endregion // Paginator Queries
    }

    /// <summary>
    /// Hides pooled wiki buttons whose tab or page is still locked. A tab is available when any of
    /// its pages is unlocked; a thumbnail is available when its own page is.
    ///
    /// Runs immediately after WikiPoolUtility.RebuildStrips, whose (TabIndex, PageIndex)
    /// assignments it reads and asserts on rather than re-validating. WikiState.OnSceneLateEnable
    /// sequences the two.
    ///
    /// Active-tab filtering and paginator-window visibility are separate, later cuts made by
    /// WikiVisualsUtility.
    /// </summary>
    public static class WikiAvailabilityUtility {
        public static void ApplyUnlocks(WikiContent content, WikiPools pools, PlayerProgressState progressState) {
            // Walk the pools' allocated sets.
            var tabButtons = pools.TabButtonPool.ActiveObjects;
            for (int i = 0; i < tabButtons.Count; i++) {
                ApplyTabAvailability(tabButtons[i], content, progressState);
            }

            var thumbButtons = pools.PageThumbPool.ActiveObjects;
            for (int i = 0; i < thumbButtons.Count; i++) {
                ApplyPageThumbAvailability(thumbButtons[i], content, progressState);
            }

            // Chrome buttons (arrows, exit, collapsed icon) need no pass — WikiButton.Available
            // initializes true and only the two helpers above ever clear it.

            // The selection can be left pointing at a tab that has no unlocked pages, either on a
            // fresh save or after content changed. Fall back to the first unlocked tab.
            WikiState wikiState = Find.State<WikiState>();
            if (wikiState != null && content.Tabs != null)
            {
                // Every button just had its visibility reassigned, so both strips need restyling.
                // SelectTab below may widen this further.
                WikiVisualsUtility.Invalidate(wikiState, WikiVisualDirty.TabStrip | WikiVisualDirty.Paginator);

                bool validTab = wikiState.ActiveTabIndex >= 0
                    && wikiState.ActiveTabIndex < content.Tabs.Length
                    && WikiUtility.IsTabUnlocked(progressState, content.Tabs[wikiState.ActiveTabIndex]);

                if (!validTab)
                {
                    for (int i = 0; i < content.Tabs.Length; i++)
                    {
                        if (WikiUtility.IsTabUnlocked(progressState, content.Tabs[i]))
                        {
                            WikiUtility.SelectTab(wikiState, content, progressState, i);
                            break;
                        }
                    }
                }
            }
        }

        // RebuildStrips assigned TabIndex from this same content immediately before, so it is in
        // range by construction.
        private static void ApplyTabAvailability(WikiButton button, WikiContent content, PlayerProgressState progressState) {
            Assert.True(button.TabIndex >= 0 && button.TabIndex < content.Tabs.Length,
                "Wiki tab button has out-of-range TabIndex {0}", button.TabIndex);

            bool available = WikiUtility.IsTabUnlocked(progressState, content.Tabs[button.TabIndex]);
            ApplyAvailability(button, available);
        }

        // Same construction guarantee as above, for both halves of the thumb's (TabIndex,
        // PageIndex) pair.
        private static void ApplyPageThumbAvailability(WikiButton button, WikiContent content, PlayerProgressState progressState) {
            Assert.True(button.TabIndex >= 0 && button.TabIndex < content.Tabs.Length,
                "Wiki page thumb has out-of-range TabIndex {0}", button.TabIndex);

            WikiTabData tab = content.Tabs[button.TabIndex];
            Assert.True(button.PageIndex >= 0 && button.PageIndex < tab.Pages.Length,
                "Wiki page thumb has out-of-range PageIndex {0} for tab '{1}'", button.PageIndex, tab.name);

            WikiPageData page = tab.Pages[button.PageIndex];
            Assert.NotNullOrDestroyed(page, "Wiki tab '{0}' has a null page at index {1}", tab.name, button.PageIndex);

            bool available = WikiUtility.IsPageUnlocked(progressState, page.AssetId);
            ApplyAvailability(button, available);
        }

        private static void ApplyAvailability(WikiButton button, bool available) {
            Assert.NotNullOrDestroyed(button.DynamicButton, "Wiki button '{0}' has no DynamicButton", button.name);

            button.Available = available;
            button.gameObject.SetActive(available);
            button.DynamicButton.enabled = available;
        }
    }
}
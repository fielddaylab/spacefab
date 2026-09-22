using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System;

namespace SpaceFab.UI {
    /// <summary>
    /// The wiki's frame bookkeeping, in two passes on two phases.
    ///
    /// Update order 0 clears the one-frame pointer flags, after WikiSelectSystem (PreUpdate 0) has
    /// consumed them.
    ///
    /// LateUpdate order 800 drains the two pending-work signals in dependency order: NeedsRebuild
    /// first, since rebuilding changes which button instances exist, then WikiState.VisualsDirty
    /// into WikiVisualsUtility.Refresh. 800 puts it behind every mutation source in the frame —
    /// WikiSelectSystem, the transition routines, and the Research property-confirm path that
    /// reaches UnlockPage at LateUpdate 60. Rendering happens after LateUpdate, so it all still
    /// lands in the same frame.
    /// </summary>
    public class WikiUpdateSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 600, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWriteShared<WikiViewState>()
                    .ReadWriteShared<WikiContent>()
                    .ReadWriteShared<WikiLayoutState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(out WikiViewState state, out WikiContent content, out WikiLayoutState layout, out PlayerProgressState playerProgress);

            WikiUtility.FlushContentChanges(state, content, playerProgress);

            PageSeekMode seekMode = PageSeekMode.NotSeeking;
            bool changedTab = false;
            bool changedPage = false;
            bool changedScroll = false;

            if (state.QueuedTabId >= 0) {
                seekMode = state.QueuedPageId >= 0 ? PageSeekMode.SpecificTabAndPage : PageSeekMode.ChangeTab;
            } else if (state.QueuedPageId >= 0) {
                seekMode = PageSeekMode.ChangePage;
            }

            // process selection changes
            if (state.QueuedTabId >= 0) {
                int tabIndex = state.QueuedTabId;
                state.QueuedTabId = -1;

                state.QueuedPageScrollDirection = 0;
                changedTab = SwapTabs(state, content, layout, tabIndex, ref seekMode);
            }

            if (state.QueuedPageId >= 0) {
                int pageId = state.QueuedPageId;
                int queuedScroll = state.QueuedPageScrollRestore;
                state.QueuedPageId = -1;
                state.QueuedPageScrollRestore = -1;

                state.QueuedPageScrollDirection = 0;
                int currentScroll = state.CurrentPageScroll;
                changedPage = SwapPages(state, content, layout, pageId, queuedScroll, seekMode);
                changedScroll |= currentScroll != state.CurrentPageScroll;
            }

            // update tabs
            if (!state.QueuedContentUpdated.AvailableTabsUpdated.IsEmpty) {
                WikiLayoutUtility.PopulateTabs(layout.Tabinator, content);
                WikiLayoutUtility.UpdateSelectedTab(layout.Tabinator, state.CurrentTabId, true);
                state.QueuedContentUpdated.AvailableTabsUpdated = default;
            } else if (changedTab) {
                WikiLayoutUtility.UpdateSelectedTab(layout.Tabinator, state.CurrentTabId, false);
            }

            // update paginator
            if (changedTab || changedScroll || (state.CurrentTabId >= 0 && state.QueuedContentUpdated.PageListsUpdated.IsSet(state.CurrentTabId))) {
                WikiLayoutUtility.PopulatePages(layout.Paginator, content, state.CurrentTabId, state.CurrentPageScroll);
                WikiLayoutUtility.UpdateSelectedPage(layout.Paginator, state.CurrentPageId, true);
                state.QueuedContentUpdated.PageListsUpdated.Unset(state.CurrentTabId);
            } else if (changedPage) {
                WikiLayoutUtility.UpdateSelectedPage(layout.Paginator, state.CurrentPageId, false);
            }

            // update page data
            if (changedPage && state.CurrentTabId >= 0) {
                WikiLayoutUtility.PopulatePageContent(layout.PageLayout, content.Tabs[state.CurrentTabId].Pages[state.CurrentPageId], content);
            }
        }

        static private bool SwapTabs(WikiViewState state, WikiContent content, WikiLayoutState layout, int tabIndex, ref PageSeekMode seekMode) {
            Assert.True(tabIndex >= 0, "Tab index out of bounds");

            if (tabIndex == state.CurrentTabId) {
                // if we'd otherwise be seeking a specific page, but we're seeking within the same tab,
                // downgrade to page change
                if (seekMode == PageSeekMode.SpecificTabAndPage) {
                    seekMode = PageSeekMode.ChangePage;
                }
                return false;
            }

            if (!content.AvailableTabs.Availability.IsSet(tabIndex)) {
                Log.Warn("[WikiUpdateState] Tab '{0}' is not available right now", content.Tabs[tabIndex].AssetId);
                return false;
            }

            int prevTabIndex = state.CurrentTabId;
            if (prevTabIndex >= 0) {
                ref WikiTabMemoryRecord oldMemoryRecord = ref state.TabMemory[prevTabIndex];
                oldMemoryRecord.PageId = (sbyte) state.CurrentPageId;
                oldMemoryRecord.Scroll = (sbyte)state.CurrentPageScroll;
            }

            state.CurrentTabId = tabIndex;

            WikiContentList tabContentList = content.TabPages[tabIndex];
            Assert.True(tabContentList.Count > 0, "Tab has no pages!");
            WikiTabMemoryRecord tabMemoryRecord = state.TabMemory[tabIndex];

            // restore from tab memory
            if (seekMode == PageSeekMode.ChangeTab) {
                state.QueuedPageId = FindFirstAvailablePage(tabContentList, tabMemoryRecord.PageId);
                state.QueuedPageScrollRestore = tabMemoryRecord.Scroll;
                state.CurrentPageId = -1;
            }

            return true;
        }

        static private bool SwapPages(WikiViewState state, WikiContent content, WikiLayoutState layout, int pageIndex, int desiredScroll, PageSeekMode seekMode) {
            Assert.True(pageIndex >= 0, "Page index out of bounds");

            int tabIndex = state.CurrentTabId;

            WikiContentList tabContentList = content.TabPages[tabIndex];
            Assert.True(tabContentList.Count > 0, "Tab has no pages!");

            int correctedPageIndex = FindFirstAvailablePage(tabContentList, pageIndex);
            int visualIndex = WikiContentUtility.GetVisualIndex(tabContentList, correctedPageIndex);
            int scroll = desiredScroll;
            if (seekMode == PageSeekMode.SpecificTabAndPage) {
                scroll = FindCenteredScroll(visualIndex, tabContentList.Count, layout.Paginator.Pages.Length);
            }

            scroll = AdjustScrollToEnsureInWindow(visualIndex, scroll, tabContentList.Count, layout.Paginator.Pages.Length);
            state.CurrentPageScroll = scroll;

            if (state.CurrentPageId != correctedPageIndex || seekMode == PageSeekMode.ChangeTab) {
                state.CurrentPageId = correctedPageIndex;
                return true;
            }

            return false;
        }

        static private unsafe int FindFirstAvailablePage(WikiContentList contentList, int targetId) {
            Assert.True(contentList.Count > 0, "Tab has no pages!");

            if (targetId < 0) {
                return contentList.Indices[0];
            }
            
            if (contentList.Availability.IsSet(targetId)) {
                return targetId;
            }

            // content list is always sorted in ascending order
            // so we don't need to check against the current closest
            int closest = 0;
            for(int i = 0; i < contentList.Count; i++) {
                int index = contentList.Indices[i];
                if (index < targetId) {
                    closest = i;
                } else {
                    break;
                }
            }
            return closest;
        }
    
        static private int FindCenteredScroll(int visualIndex, int count, int windowSize) {
            int lower = visualIndex - windowSize / 2;
            return Math.Max(0, Math.Min(lower, count - windowSize));
        }

        static private int AdjustScrollToEnsureInWindow(int visualIndex, int currentScroll, int count, int windowSize) {
            int lower = currentScroll;
            int upper = currentScroll + windowSize;

            int unbounded;
            if (visualIndex >= upper) {
                unbounded = visualIndex + 1 - windowSize;
            } else if (visualIndex < lower) {
                unbounded = visualIndex;
            } else {
                unbounded = currentScroll;
            }

            return Math.Max(0, Math.Min(unbounded, count - windowSize));
        } 
    
        private enum PageSeekMode {
            NotSeeking,
            ChangeTab,
            ChangePage,
            SpecificTabAndPage
        }
    }
}

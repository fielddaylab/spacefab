using FieldDay;
using FieldDay.Systems;
using FieldDay.SharedState;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace SpaceFab.UI {
    /// <summary>
    /// Drives the wiki's presentation layer from WikiState + WikiContent: expanded/collapsed
    /// root visibility, active-tab highlight, page content binding, and paginator arrow enable-
    /// state.
    ///
    /// Runs on PreUpdate at order 10 under WikiMask — after WikiSelectSystem has finished
    /// mutating state and before WikiRefreshSystem (Update order 0) clears the one-frame
    /// button flags.
    ///
    /// Body is currently stubbed. Fields and permissions are declared so the consuming prefab
    /// can be built against a stable shape.
    /// </summary>
    public class WikiVisualsUpdateSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 10, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadShared<WikiState>()
                    .ReadShared<WikiLayoutState>()
                    .Read<WikiContent>()
                    .Read<WikiPools>()
                    .ReadShared<PlayerProgressState>()
                    .ReadWrite<WikiButton>()
            );
        }

        // TODO: implement visuals refresh.
        //
        // Authoring note: the tab + page-thumb button instances are NOT spawned here. They are
        // pooled via WikiPools and rebuilt by WikiPoolUtility.RebuildStrips, which is invoked
        // by WikiAvailabilityUtility.ApplyUnlocks on level-load and after every
        // WikiUtility.UnlockPage mutation. This system only reads the already-spawned set and
        // applies per-frame visual state.
        //
        // Rough shape:
        //   WikiState wikiState = Find.State<WikiState>();
        //   PlayerProgressState progressState = Find.State<PlayerProgressState>();
        //   WikiContent content = Find.Components<WikiContent>()[0];
        //   WikiPools pools = Find.Components<WikiPools>()[0];
        //
        //   1. Expanded vs. collapsed roots. When Transitioning == false, the Expanded bool
        //      decides which of the two sibling CanvasGroups (ExpandedRoot, CollapsedRoot) is
        //      fully visible. During Transitioning, a BeauRoutine tween chained into
        //      ExpandRoutine / CollapseRoutine drives the alpha — this system only asserts the
        //      steady-state endpoint.
        //
        //   2. Tab highlight. Walk pools.TabActive; on each tab button, update a "selected"
        //      visual (outline / color) based on whether button.TabIndex == wikiState
        //      .ActiveTabIndex. Availability was already applied by
        //      WikiAvailabilityUtility.ApplyUnlocks at rebuild time.
        //
        //   3. Page content. Pull content.Tabs[ActiveTabIndex].Pages[ActivePageIndex] and push
        //      Title → WikiPageContentWidgets.TitleText.text, Body → BodyText.text, Illustration
        //      → IllustrationImage.sprite. Deactivate IllustrationImage.gameObject when the
        //      page has no illustration (page.Illustration == null).
        //
        //   4. Paginator strip. Walk pools.PageThumbActive. For each thumb:
        //        - if thumb.TabIndex != wikiState.ActiveTabIndex: hide (gameObject inactive),
        //          this thumb belongs to an inactive tab.
        //        - else compute slot = WikiUtility.GetUnlockedIndex(activeTab, progressState,
        //          thumb.PageIndex). If slot == -1 the thumb is locked — hide it.
        //        - visible iff slot in [PageWindowStartIndex, PageWindowStartIndex + PageWindowSize).
        //      Per-thumb: set the thumbnail Image sprite from activeTab.Pages[thumb.PageIndex]
        //      .Icon, highlight the thumb whose PageIndex == ActivePageIndex. The PaginatorContent
        //      RectTransform slides by anchoredPosition.x = -PageWindowStartIndex * iconStride
        //      so the UI Mask on the strip clips out-of-window icons automatically.
        //
        //   5. Paginator arrow enable state. PrevPage.interactable = CanScrollPageWindowLeft;
        //      NextPage.interactable = CanScrollPageWindowRight. Arrows stay visible at the
        //      ends (layout stays stable) but the `DynamicButton.interactable = false` plus
        //      a greyed-out sprite swap signals the disabled state.
        static private void ProcessWork(float deltaTime)
        {
            // 1.
            Find.State(
                out WikiState wikiState,
                out WikiLayoutState layoutState,
                out PlayerProgressState progressState
                );
                
            if (Find.Components<WikiContent>().Count == 0) { return; }
            WikiContent content = Find.Components<WikiContent>()[0];

            if (Find.Components<WikiPools>().Count == 0) { return; }
            WikiPools pools = Find.Components<WikiPools>()[0];

            if (!wikiState.Transitioning) {
                WikiLayoutUtility.ApplyExpandedSteadyState(layoutState, wikiState.Expanded);
            }

            if (!wikiState.Expanded) {
                return;
            }

            // 2.
            for (int i = 0; i < pools.TabActive.Count; i++) {
                WikiButton tab = pools.TabActive[i];
                if (tab.DynamicButton == null) { continue; }
                bool selected = tab.TabIndex == wikiState.ActiveTabIndex;

                if (tab.DynamicButton != null) {
                    tab.DynamicButton.image.color = selected ?
                        tab.DynamicButton.colors.highlightedColor :
                        tab.DynamicButton.colors.normalColor;
                }
            }

            // 3.
            WikiTabData activeTab = null;
            if (content.Tabs != null && wikiState.ActiveTabIndex >= 0 && wikiState.ActiveTabIndex < content.Tabs.Length)
            {
                activeTab = content.Tabs[wikiState.ActiveTabIndex];
            }

            WikiPageData activePage = null;
            if (activeTab != null && activeTab.Pages != null && wikiState.ActivePageIndex >= 0 && wikiState.ActivePageIndex < activeTab.Pages.Length)
            {
                activePage = activeTab.Pages[wikiState.ActivePageIndex];
                Debug.Log("ActivePage: " + activeTab.Pages[wikiState.ActivePageIndex].Title);
            }

            if (layoutState.PageContentWidgets != null && activePage != null)
            {
                var widgets = layoutState.PageContentWidgets;

                if (widgets.TitleText != null)
                    widgets.TitleText.text = activePage.Title ?? string.Empty;
                    Debug.Log("Title: " + widgets.TitleText.text);

                if (widgets.BodyText != null)
                    widgets.BodyText.text = activePage.Body ?? string.Empty;
                    Debug.Log("Body: " + widgets.BodyText.text);

                if (widgets.IllustrationImage != null) {
                    bool hasIllustration = activePage.Illustration != null;
                    widgets.IllustrationImage.sprite = hasIllustration ? activePage.Illustration : null;
                    widgets.IllustrationImage.gameObject.SetActive(hasIllustration);
                }
            }

            // 4.
            for (int i = 0; i < pools.PageThumbActive.Count; i++) {
                WikiButton thumb = pools.PageThumbActive[i];
                if (thumb.DynamicButton == null) { continue; }

                bool belongsToActiveTab = thumb.TabIndex == wikiState.ActiveTabIndex;
                if (!belongsToActiveTab) {
                    thumb.gameObject.SetActive(false);
                    continue;
                }

                int unlockedIndex = WikiUtility.GetUnlockedIndex(activeTab, progressState, thumb.PageIndex);
                bool isLocked = unlockedIndex == -1;
                if (isLocked) {
                    thumb.gameObject.SetActive(false);
                    continue;
                }

                bool inWindow = unlockedIndex >= wikiState.PageWindowStartIndex && unlockedIndex < wikiState.PageWindowStartIndex + content.PageWindowSize;
                thumb.gameObject.SetActive(inWindow);

                if (thumb.DynamicButton != null) {
                    thumb.DynamicButton.image.color = thumb.PageIndex == wikiState.ActivePageIndex ?
                        thumb.DynamicButton.colors.highlightedColor :
                        thumb.DynamicButton.colors.normalColor;
                }
            }

            // 5.
            if (layoutState.PrevPage != null) {
                layoutState.PrevPage.interactable = WikiUtility.CanScrollPageWindowLeft(wikiState);
            }
            if (layoutState.NextPage != null) {
                layoutState.NextPage.interactable = WikiUtility.CanScrollPageWindowRight(wikiState, content, progressState);
            }
        }
    }
}

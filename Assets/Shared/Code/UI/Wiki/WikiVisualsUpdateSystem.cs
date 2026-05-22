using FieldDay;
using FieldDay.Systems;
using FieldDay.SharedState;
using SpaceFab.Research;
using UnityEngine.UI;
using UnityEngine;
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

            WikiLayoutUtility.ScrollPaginator(layoutState, wikiState.PageWindowStartIndex);

            // 2.
            for (int i = 0; i < pools.TabActive.Count; i++) {
                WikiButton tab = pools.TabActive[i];
                var tabContent = tab.transform.Find("Tab Content");

                if (tabContent != null) {
                    var icon = tabContent.GetComponent<Image>();
                    
                    if (icon != null && content.Tabs != null && tab.TabIndex >= 0 && tab.TabIndex < content.Tabs.Length) {
                        icon.sprite = content.Tabs[tab.TabIndex].Icon;
                        icon.color = Color.white;
                    }
                }

                if (tab.DynamicButton == null) { continue; }
                bool selected = tab.TabIndex == wikiState.ActiveTabIndex;

                if (tab.DynamicButton != null) {
                    tab.DynamicButton.image.sprite = selected ? layoutState.TabActiveSprite : layoutState.TabInactiveSprite;
                    tab.DynamicButton.image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, selected ? 70f : 65f); // temp visual highlight since the active tab's text is invisible until the page content is implemented
                    tab.DynamicButton.interactable = true; // content.Tabs[tab.TabIndex].Unlocked;
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
            }

            if (layoutState.PageContentWidgets != null && activePage != null)
            {
                var widgets = layoutState.PageContentWidgets;
                bool materialPage = activePage.IsMaterialPage;

                // Title always renders.
                if (widgets.TitleText != null) {
                    widgets.TitleText.text = activePage.Title ?? " ";
                }

                // Body wrapper visible only on default pages.
                if (widgets.DefaultGroup != null) {
                    widgets.DefaultGroup.SetActive(!materialPage);
                }
                if (!materialPage && widgets.BodyText != null) {
                    widgets.BodyText.text = activePage.Body ?? " ";
                }

                // Characteristics group visible only on material pages.
                // Chip allocation is owned by
                // WikiCharacteristicsRefreshSystem, which runs at
                // PreUpdate 5 (before this system at PreUpdate 10).
                if (widgets.MaterialCharacteristicsGroup != null) {
                    widgets.MaterialCharacteristicsGroup.SetActive(materialPage);
                }

                // Illustration source depends on page kind. Default
                // pages use the authored sprite; material pages pull
                // from the material's ResearchMaterialView.
                if (widgets.IllustrationImage != null) {
                    Sprite illustration = null;
                    if (materialPage) {
                        ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(activePage.MaterialId);
                        if (view != null) {
                            illustration = view.IsMultiAtom ? view.MultiAtomSprite : view.SingleAtomSprite;
                        }
                    } else {
                        illustration = activePage.Illustration;
                    }
                    bool hasIllustration = illustration != null;
                    widgets.IllustrationImage.sprite = hasIllustration ? illustration : null;
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
                // if (isLocked) {
                //     thumb.gameObject.SetActive(false);
                //     continue;
                // }

                int pageWindowSize = Mathf.Max(1, content.PageWindowSize);
                bool inWindow = unlockedIndex >= wikiState.PageWindowStartIndex && unlockedIndex < wikiState.PageWindowStartIndex + pageWindowSize;
                thumb.gameObject.SetActive(true);
                if (thumb.DynamicButton == null) { continue; }

                thumb.DynamicButton.interactable = true; // isLocked ? false : true;
                // Material pages pull their thumbnail from the
                // ResearchMaterialView (same single/multi-atom
                // selection as the page illustration), not from
                // WikiPageData.Icon. Default pages keep using the
                // authored icon.
                WikiPageData thumbPage = activeTab.Pages[thumb.PageIndex];
                Sprite thumbSprite = thumbPage != null ? thumbPage.Icon : null;
                if (thumbPage != null && thumbPage.IsMaterialPage) {
                    ResearchMaterialView thumbView = Find.NamedAsset<ResearchMaterialView>(thumbPage.MaterialId);
                    if (thumbView != null) {
                        thumbSprite = thumbView.IsMultiAtom ? thumbView.MultiAtomSprite : thumbView.SingleAtomSprite;
                    }
                }
                thumb.DynamicButton.image.sprite = thumbSprite;
                thumb.DynamicButton.image.color = thumb.PageIndex == wikiState.ActivePageIndex ?
                    Color.magenta : // highlight active page thumb in yellow for testing
                    Color.cyan;
            }

            // 5.
            if (layoutState.PrevPage != null) {
                layoutState.PrevPage.interactable = WikiUtility.CanScrollPageWindowLeft(wikiState, content, progressState);
            }
            if (layoutState.NextPage != null) {
                layoutState.NextPage.interactable = WikiUtility.CanScrollPageWindowRight(wikiState, content, progressState);
            }
        }
    }
}

using FieldDay;
using FieldDay.Systems;

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
        static private void ProcessWork(float deltaTime) {
        }
    }
}

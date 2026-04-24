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
                    .ReadShared<PlayerProgressState>()
                    .ReadWrite<WikiButton>()
            );
        }

        // TODO: implement visuals refresh.
        //
        // Rough shape:
        //   WikiState wikiState = Find.State<WikiState>();
        //   PlayerProgressState progressState = Find.State<PlayerProgressState>();
        //   WikiContent content = Find.Components<WikiContent>()[0];
        //
        //   1. Expanded vs. collapsed roots. When Transitioning == false, the Expanded bool
        //      decides which of the two sibling CanvasGroups (ExpandedRoot, CollapsedRoot) is
        //      fully visible. During Transitioning, a BeauRoutine tween chained into
        //      ExpandRoutine / CollapseRoutine drives the alpha — this system only asserts the
        //      steady-state endpoint.
        //
        //   2. Tab highlight. Walk Find.Components<WikiButton>(); on each Kind==Tab button,
        //      update a "selected" visual (outline / color) based on whether
        //      button.TabIndex == wikiState.ActiveTabIndex. Also mirror button.Available onto
        //      gameObject.activeSelf (WikiAvailabilityUtility already does this on unlock-
        //      state changes, but re-asserting here catches inspector edits during play).
        //
        //   3. Page content. Pull content.Tabs[ActiveTabIndex].Pages[ActivePageIndex] and push
        //      Title → WikiPageContentWidgets.TitleText.text, Body → BodyText.text, Illustration
        //      → IllustrationImage.sprite. Deactivate IllustrationImage.gameObject when the
        //      page has no illustration (page.Illustration == null).
        //
        //   4. Paginator strip. For each raw page button in the strip:
        //        - int slot = WikiUtility.GetUnlockedIndex(tab, progressState, rawIndex);
        //        - visible iff slot in [PageWindowStartIndex, PageWindowStartIndex + PageWindowSize).
        //      Locked pages have slot == -1 and are permanently hidden (parity with
        //      ApplyUnlocks' locked-tab treatment).
        //      Per-button: set WikiButton.Icon sprite from page.Icon, highlight the thumbnail
        //      whose rawIndex == ActivePageIndex. The content RectTransform slides by
        //      anchoredPosition.x = -PageWindowStartIndex * iconStride so a UI Mask on the
        //      strip clips out-of-window icons automatically.
        //
        //   5. Paginator arrow enable state. PrevPage.interactable = CanScrollPageWindowLeft;
        //      NextPage.interactable = CanScrollPageWindowRight. Arrows stay visible at the
        //      ends (layout stays stable) but the `DynamicButton.interactable = false` plus
        //      a greyed-out sprite swap signals the disabled state.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

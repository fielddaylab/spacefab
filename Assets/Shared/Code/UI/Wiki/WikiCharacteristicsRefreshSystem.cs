using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Research;

namespace SpaceFab.UI {
    /// <summary>
    /// Rebuilds the material-characteristics chip column when:
    ///   - The active wiki page just changed
    ///     (WikiState.ActivePageChangedThisFrame), OR
    ///   - The Research minigame is loaded and just confirmed a
    ///     property (ResearchMinigameState.PropertyConfirmedThisFrame).
    ///
    /// On a default (non-material) page, frees any leftover chips
    /// from a prior material page so the pool returns to baseline.
    ///
    /// Two scheduling entries:
    ///   - PreUpdate order 5: catches the page-change path. Runs
    ///     after WikiSelectSystem (PreUpdate 0) which raised
    ///     ActivePageChangedThisFrame, and before
    ///     WikiVisualsUpdateSystem (PreUpdate 10) which flips group
    ///     visibility. Chips are populated before the visuals system
    ///     enables the group, so there's no empty-group flicker on
    ///     navigation.
    ///   - LateUpdate order 700: catches the property-confirmed
    ///     path. Runs after HypothesisSubmitSystem (LateUpdate 60)
    ///     which calls TryConfirmHypothesis → bridge sets
    ///     PropertyConfirmedThisFrame, and before
    ///     ResearchMinigameStateRefreshSystem (LateUpdate 1000)
    ///     which clears the flag. The new property appears in the
    ///     chip column the same frame it's confirmed.
    ///
    /// Same ProcessWork body services both: the gate checks both
    /// flags and either rebuilds or returns.
    /// </summary>
    public class WikiCharacteristicsRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            // Page-change pass — chips ready before group visibility flips.
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 5, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadShared<WikiState>()
                    .ReadShared<WikiLayoutState>()
                    .ReadWriteShared<WikiChipPools>()
                    .Read<WikiContent>()
            );
            // Property-confirmed pass — runs after Research confirm,
            // before the frame-flag clear. Lets a mid-Research
            // confirmation flow into the open wiki page the same
            // frame.
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 700, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadShared<WikiState>()
                    .ReadShared<WikiLayoutState>()
                    .ReadWriteShared<WikiChipPools>()
                    .Read<WikiContent>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out WikiState wikiState,
                out WikiLayoutState layoutState,
                out WikiChipPools pools
            );

            if (wikiState == null || layoutState == null || pools == null) return;
            WikiPageContentWidgets widgets = layoutState.PageContentWidgets;
            if (widgets == null) return;

            // Resolve the active page to know whether it's a material
            // page at all.
            if (Find.Components<WikiContent>().Count == 0) return;
            WikiContent content = Find.Components<WikiContent>()[0];

            WikiPageData activePage = ResolveActivePage(wikiState, content);

            // Gate: refresh only when something changed. If neither
            // trigger fired this frame, leave the existing chip set
            // alone.
            ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
            bool propertyConfirmed = researchState != null && researchState.PropertyConfirmedThisFrame;
            bool pageChanged = wikiState.ActivePageChangedThisFrame;
            if (!propertyConfirmed && !pageChanged) return;

            if (activePage == null || !activePage.IsMaterialPage) {
                // Navigated to a default page (or no active page).
                // Free any leftover chips from a prior material page
                // so the pool returns to baseline.
                WikiCharacteristicsLoadUtility.FreeAllCharacteristicChips(pools);
                return;
            }

            // Material page: rebuild the chip column from the merged
            // confirmed-property record.
            WikiCharacteristicsLoadUtility.LoadFor(widgets, pools, activePage.MaterialId);
        }

        // Resolves the active page from the (TabIndex, PageIndex) on
        // WikiState. Returns null if either index is out of range or
        // content isn't authored.
        private static WikiPageData ResolveActivePage(WikiState wikiState, WikiContent content) {
            if (content == null || content.Tabs == null) return null;
            int tabIdx = wikiState.ActiveTabIndex;
            if (tabIdx < 0 || tabIdx >= content.Tabs.Length) return null;
            WikiTabData tab = content.Tabs[tabIdx];
            if (tab == null || tab.Pages == null) return null;
            int pageIdx = wikiState.ActivePageIndex;
            if (pageIdx < 0 || pageIdx >= tab.Pages.Length) return null;
            return tab.Pages[pageIdx];
        }
    }
}

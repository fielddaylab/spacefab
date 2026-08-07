using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Research;

namespace SpaceFab.UI {
    /// <summary>
    /// Rebuilds the material-characteristics chip column when the active wiki page changes, or
    /// when the Research minigame confirms a property. On a default (non-material) page, frees any
    /// chips left over from a prior material page.
    ///
    /// The same ProcessWork body is registered twice because the two triggers fire in different
    /// phases; the gate at the top checks both flags and returns when neither is raised.
    /// </summary>
    public class WikiCharacteristicsRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            // Page-change pass. Runs after WikiSelectSystem (PreUpdate 0) raises
            // ActivePageChangedThisFrame, still ahead of render, so the column is never seen empty.
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 5, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadShared<WikiState>()
                    .ReadShared<WikiLayoutState>()
                    .ReadWriteShared<WikiChipPools>()
                    .Read<WikiContent>()
            );
            // Property-confirmed pass. Slots between HypothesisSubmitSystem (LateUpdate 60), which
            // raises PropertyConfirmedThisFrame, and ResearchMinigameStateRefreshSystem
            // (LateUpdate 1000), which clears it — so a confirmation reaches an already-open wiki
            // page on the frame it happens.
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

            if (Find.Components<WikiContent>().Count == 0) return;
            WikiContent content = Find.Components<WikiContent>()[0];

            WikiPageData activePage = ResolveActivePage(wikiState, content);

            // Neither trigger fired means the existing chip set is still correct.
            bool propertyConfirmed = false;
            if (Game.SharedState.Has<ResearchMinigameState>())
            {
                ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
                propertyConfirmed = researchState.PropertyConfirmedThisFrame;
            }
            bool pageChanged = wikiState.ActivePageChangedThisFrame;
            if (!propertyConfirmed && !pageChanged) return;

            // A default page has no chip column, so return the pool to baseline.
            if (activePage == null || !activePage.IsMaterialPage) {
                WikiCharacteristicsLoadUtility.FreeAllCharacteristicChips(pools);
                return;
            }

            WikiCharacteristicsLoadUtility.LoadFor(widgets, pools, activePage.MaterialId);
        }

        // Resolves the page WikiState's (ActiveTabIndex, ActivePageIndex) points at, or null if
        // either index is out of range or content isn't authored.
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

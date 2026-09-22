using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;

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
    public class WikiRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWrite<WikiButton>()
            );

            ecs.Register(&DrainPendingWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 800, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWrite<WikiButton>()
                    .ReadWrite<WikiPools>()
                    .Read<WikiContent>()
                    .ReadWriteShared<WikiState>()
                    .ReadWriteShared<WikiLayoutState>()
                    .ReadWriteShared<WikiChipPools>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            var buttons = Find.Components<WikiButton>();
            for (int i = 0; i < buttons.Count; i++) {
                buttons[i].ClickedThisFrame = false;
                buttons[i].PointerEnterThisFrame = false;
                buttons[i].PointerExitThisFrame = false;
            }
        }

        // Rebuilds the strips if their contents changed, then applies whatever the frame's mutations
        // invalidated. The whole cost when nothing changed is the two compares at the top.
        static private void DrainPendingWork(float deltaTime) {
            Find.State(
                out WikiState wikiState,
                out WikiLayoutState layoutState,
                out WikiChipPools chipPools,
                out PlayerProgressState progressState
                );

            bool needsRebuild = wikiState.NeedsRebuild;
            if (!needsRebuild && wikiState.VisualsDirty == WikiVisualDirty.None) { return; }

            // No WikiContent means this scene doesn't ship the wiki prefab, so there's nothing to
            // rebuild or paint. Everything past this point assumes the full authoring is present.
            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }
            WikiContent content = contents[0];

            var pools = Find.Components<WikiPools>();
            Assert.True(pools.Count > 0, "WikiPools missing from a scene that has WikiContent");

            // Structural first: a rebuild changes which instances exist, and both halves invalidate
            // the strip domains on their own, so the paint below picks the change up.
            if (needsRebuild) {
                WikiPoolUtility.RebuildStrips(wikiState, content, pools[0]);
                WikiAvailabilityUtility.ApplyUnlocks(content, pools[0], progressState);
                wikiState.NeedsRebuild = false;
            }

            // Research state is resolved here rather than declared in SysPermissions: it exists
            // only in the Research scene, while this system runs under WikiMask in every minigame.
            // Resolve returns an absent context elsewhere, and the page binds render inert.
            WikiResearchContext researchContext = WikiResearchContextUtility.Resolve();

            WikiVisualsUtility.Refresh(wikiState, layoutState, content, pools[0], chipPools, progressState, researchContext);

            // Announced after the paint, so OnWikiTabOpened / OnWikiPageOpened fire against what is
            // actually on screen, once for wherever the frame's mutations left the selection. Safe
            // to sit behind the early return above: every path that moves the selection invalidates
            // a domain, so a frame with nothing dirty has nothing new to announce either.
            WikiUtility.AnnounceSelection(wikiState, content);
        }
    }
}

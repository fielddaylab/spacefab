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
    /// LateUpdate order 800 drains WikiState.VisualsDirty into WikiVisualsUtility.Refresh. 800 puts
    /// it behind every mutation source in the frame: WikiSelectSystem, the transition routines,
    /// the Research property-confirm path that reaches UnlockPage (LateUpdate 60), and
    /// WikiCharacteristicsRefreshSystem (LateUpdate 700). Trailing 700 matters — chips are filled
    /// before this pass flips the characteristics group visible, so navigation never shows an empty
    /// column. Rendering happens after LateUpdate, so it all still lands in the same frame.
    ///
    /// The two passes must stay on separate phases. The Update 0 clear of ActivePageChangedThisFrame
    /// is what keeps WikiCharacteristicsRefreshSystem's LateUpdate 700 entry scoped to the
    /// property-confirm path; moving it to LateUpdate would expose the page-change path there as
    /// well and rebuild the chip column twice per navigation.
    /// </summary>
    public class WikiRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWrite<WikiButton>()
                    .ReadWriteShared<WikiState>()
            );

            ecs.Register(&DrainVisuals,
                new SysUpdate(GameLoopPhase.LateUpdate, 800, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWriteShared<WikiState>()
                    .ReadWriteShared<WikiLayoutState>()
                    .Read<WikiContent>()
                    .Read<WikiPools>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            var buttons = Find.Components<WikiButton>();
            for (int i = 0; i < buttons.Count; i++) {
                buttons[i].ClickedThisFrame = false;
                buttons[i].PointerEnterThisFrame = false;
                buttons[i].PointerExitThisFrame = false;
            }

            // Consumed by WikiCharacteristicsRefreshSystem at PreUpdate 5 on the frame it's raised;
            // cleared here so the signal stays one-shot.
            WikiState wikiState = Find.State<WikiState>();
            if (wikiState != null) {
                wikiState.ActivePageChangedThisFrame = false;
            }
        }

        // Applies whatever the frame's mutations invalidated. The whole cost when nothing changed is
        // the enum compare below.
        static private void DrainVisuals(float deltaTime) {
            Find.State(
                out WikiState wikiState,
                out WikiLayoutState layoutState,
                out PlayerProgressState progressState
                );

            if (wikiState.VisualsDirty == WikiVisualDirty.None) { return; }

            // No WikiContent means this scene doesn't ship the wiki prefab, so there's nothing to
            // paint. Everything past this point assumes the full authoring is present.
            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }

            var pools = Find.Components<WikiPools>();
            Assert.True(pools.Count > 0, "WikiPools missing from a scene that has WikiContent");

            WikiVisualsUtility.Refresh(wikiState, layoutState, contents[0], pools[0], progressState);
        }
    }
}

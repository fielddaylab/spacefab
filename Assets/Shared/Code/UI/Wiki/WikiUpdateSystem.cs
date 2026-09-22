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
    public class WikiUpdateSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWriteShared<WikiStateV2>()
                    .ReadWriteShared<WikiContent>()
                    .ReadWriteShared<WikiLayoutState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(out WikiStateV2 state, out WikiContent content, out WikiLayoutState layout, out PlayerProgressState playerProgress);

            // update content lists
            if (state.ContentDirty) {
                WikiContentUpdateResult result = WikiUtility.UpdateAvailableContent(content, playerProgress);
                state.QueuedContentUpdated.AvailableTabsUpdated |= result.AvailableTabsUpdated;
                state.QueuedContentUpdated.PageListsUpdated |= result.PageListsUpdated;
                state.ContentDirty = false;
            }

            // process selection changes
            if (state.QueuedTabId != state.CurrentTabId) {
                if (content.AvailableTabs.Availability.IsSet(state.QueuedTabId)) {
                    state.CurrentTabId = state.QueuedTabId;
                } else {

                }

                    state.CurrentTabId = state.QueuedTabId;
            }

            // update visuals

            // update tabs
            if (!state.QueuedContentUpdated.AvailableTabsUpdated.IsEmpty) {
                WikiUtility.PopulateTabs(layout.Tabinator, content);
                WikiUtility.SetSelectedTabId(layout.Tabinator, state.CurrentTabId, false);
                state.QueuedContentUpdated.AvailableTabsUpdated = default;
            }
        }
    }
}

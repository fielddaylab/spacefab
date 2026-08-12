using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.UI {
    /// <summary>
    /// Routes per-button pointer flags and the state-level open/close/openTo requests into
    /// WikiState mutations and expand/collapse transitions.
    ///
    /// Request flags are cleared inline the moment they're consumed rather than at end of frame,
    /// so state is consistent by the time this system finishes.
    ///
    /// Nothing is repainted here. The WikiUtility mutators invoked below record which presentation
    /// domains they invalidated on WikiState.VisualsDirty, and WikiRefreshSystem drains that on
    /// LateUpdate.
    /// </summary>
    public class WikiSelectSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWrite<WikiButton>()
                    .Read<WikiContent>()
                    .ReadWriteShared<WikiState>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out WikiState wikiState,
                out PlayerProgressState progressState
                );

            // No WikiContent means this scene doesn't ship the wiki prefab, so there's nothing to
            // select into. This is the one tolerated absence check in the wiki module — everything
            // past it assumes the full authoring is present and asserts if it isn't.
            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }
            WikiContent content = contents[0];

            // 1. Request flags first, so a same-frame OpenTo-then-click resolves in the expected
            //    order: the open wins, then clicks are evaluated against the now-expanded state.
            ApplyExternalRequests(wikiState, content, progressState);

            // 2. Walk buttons in three passes — exit, enter, click — matching the toolbar's
            //    ordering so a same-frame exit-into-new-button doesn't leak stale hover state.
            //    The hover passes are scaffolding for a future highlight.
            var buttons = Find.Components<WikiButton>();

            for (int i = 0; i < buttons.Count; i++) {
                // if (!buttons[i].Available) { continue; }
                if (buttons[i].PointerExitThisFrame) {
                    // TODO: hover-exit visual hint.
                }
            }

            for (int i = 0; i < buttons.Count; i++) {
                // if (!buttons[i].Available) { continue; }
                if (buttons[i].PointerEnterThisFrame) {
                    // TODO: hover-enter visual hint.
                }
            }

            for (int i = 0; i < buttons.Count; i++) {
                // if (!buttons[i].Available) { continue; } // todo: Investigate why this is false
                if (!buttons[i].ClickedThisFrame) { continue; }

                DispatchClick(wikiState, content, progressState, buttons[i]);
                wikiState.NeedsRebuild = true;
            }
        }

        // Clears each of the three request flags as it consumes it, routing open/close through the
        // shared transition guards and applying OpenTo's tab/page selection.
        static private void ApplyExternalRequests(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            if (wikiState.OpenRequestedThisFrame) {
                wikiState.OpenRequestedThisFrame = false;
                WikiUtility.BeginExpand(wikiState);
            }

            if (wikiState.CloseRequestedThisFrame) {
                wikiState.CloseRequestedThisFrame = false;
                WikiUtility.BeginCollapse(wikiState);
            }

            if (wikiState.OpenToRequestedThisFrame) {
                wikiState.OpenToRequestedThisFrame = false;

                // Selection first, so an already-expanded panel picks up the new page in this
                // frame's drain rather than a frame later. Both setters invalidate their own
                // domains.
                WikiUtility.SelectTabById(wikiState, content, progressState, wikiState.RequestedTabId);
                WikiUtility.SelectPageById(wikiState, content, progressState, wikiState.RequestedPageId);

                // Then expand if needed.
                WikiUtility.BeginExpand(wikiState);

                wikiState.RequestedTabId = default;
                wikiState.RequestedPageId = default;
            }
        }

        // Routes a click into the matching WikiUtility command. Exit and CollapsedIcon leave their
        // guard to BeginCollapse / BeginExpand, so rapid clicks can't stack transitions.
        static private void DispatchClick(WikiState wikiState, WikiContent content, PlayerProgressState progressState, WikiButton button) {
            switch (button.Kind) {
                case WikiButtonKind.Tab:
                    WikiUtility.SelectTab(wikiState, content, progressState, button.TabIndex);
                    break;

                case WikiButtonKind.PageThumb:
                    WikiUtility.SelectPage(wikiState, content, progressState, button.PageIndex);
                    break;

                case WikiButtonKind.PageNext:
                    WikiUtility.NextPage(wikiState, content, progressState);
                    break;

                case WikiButtonKind.PagePrev:
                    WikiUtility.PrevPage(wikiState, content, progressState);
                    break;

                case WikiButtonKind.Exit:
                    WikiUtility.BeginCollapse(wikiState);
                    break;

                case WikiButtonKind.CollapsedIcon:
                    WikiUtility.BeginExpand(wikiState);
                    break;
            }
        }
    }
}

using BeauRoutine;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Consumes per-button pointer flags + state-level open/close/openTo request flags and
    /// routes them into WikiState mutations and expand/collapse routines. Runs on PreUpdate
    /// order 0 under WikiMask; WikiVisualsUpdateSystem (PreUpdate order 10) reads the resulting
    /// state this same frame.
    ///
    /// External request flags are cleared inline the moment they're consumed — not at end of
    /// frame — so the state visible to the visuals system is already consistent.
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

            // Resolve the per-minigame content root. If no WikiContent is present in this scene,
            // there's nothing to select into — skip the frame.
            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }
            WikiContent content = contents[0];

            // 1. Apply external request flags first so a same-frame OpenTo-then-click resolves
            //    in the expected order (open wins, then button clicks are evaluated against the
            //    now-expanded state).
            ApplyExternalRequests(wikiState, content, progressState);

            // 2. Walk buttons in three passes — exit, enter, click — matching the toolbar's
            //    ordering so same-frame exit-into-new-button doesn't leak stale hover state.
            //    Hover handlers are no-ops for now; wired as the shape for a future highlight
            //    pass.
            var buttons = Find.Components<WikiButton>();

            for (int i = 0; i < buttons.Count; i++) {
                // if (!buttons[i].Available) { continue; }
                if (buttons[i].PointerExitThisFrame) {
                    // TODO: hover-exit visual hint. Scaffold no-op.
                }
            }

            for (int i = 0; i < buttons.Count; i++) {
                // if (!buttons[i].Available) { continue; }
                if (buttons[i].PointerEnterThisFrame) {
                    // TODO: hover-enter visual hint. Scaffold no-op.
                }
            }

            for (int i = 0; i < buttons.Count; i++) {
                // if (!buttons[i].Available) { continue; } // todo: Investigate why this is false
                if (!buttons[i].ClickedThisFrame) { continue; }

                DispatchClick(wikiState, content, progressState, buttons[i]);
                wikiState.NeedsRebuild = true;
            }
        }

        // Consumes the three one-frame external flags on WikiState, starts the appropriate
        // transition routines via BeauRoutine.Replace, and applies tab/page selection for
        // OpenTo. Clears each flag inline on consumption.
        static private void ApplyExternalRequests(WikiState wikiState, WikiContent content, PlayerProgressState progressState) {
            if (wikiState.OpenRequestedThisFrame) {
                wikiState.OpenRequestedThisFrame = false;
                if (!wikiState.Expanded && !wikiState.Transitioning) {
                    wikiState.TransitionRoutine.Replace(WikiUtility.ExpandRoutine(wikiState));
                }
            }

            if (wikiState.CloseRequestedThisFrame) {
                wikiState.CloseRequestedThisFrame = false;
                if (wikiState.Expanded && !wikiState.Transitioning) {
                    wikiState.TransitionRoutine.Replace(WikiUtility.CollapseRoutine(wikiState));
                }
            }

            if (wikiState.OpenToRequestedThisFrame) {
                wikiState.OpenToRequestedThisFrame = false;

                // Apply the tab + page selection first so if the panel is already expanded, the
                // visuals system sees the new selection on the same frame as the request.
                WikiUtility.SelectTabById(wikiState, content, progressState, wikiState.RequestedTabId);
                WikiUtility.SelectPageById(wikiState, content, progressState, wikiState.RequestedPageId);

                // Then expand if needed.
                if (!wikiState.Expanded && !wikiState.Transitioning) {
                    wikiState.TransitionRoutine.Replace(WikiUtility.ExpandRoutine(wikiState));
                }

                wikiState.RequestedTabId = default;
                wikiState.RequestedPageId = default;
            }
        }

        // Routes a click on an Available WikiButton into the corresponding WikiUtility command.
        // Exit / CollapsedIcon are gated by Transitioning so rapid-clicks don't queue stacked
        // transitions.
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
                    if (wikiState.Expanded && !wikiState.Transitioning) {
                        wikiState.TransitionRoutine.Replace(WikiUtility.CollapseRoutine(wikiState));
                    }
                    break;

                case WikiButtonKind.CollapsedIcon:
                    if (!wikiState.Expanded && !wikiState.Transitioning) {
                        wikiState.TransitionRoutine.Replace(WikiUtility.ExpandRoutine(wikiState));
                    }
                    break;
            }
        }
    }
}

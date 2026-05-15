using BeauPools;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Renders every active ResearchHypothesisPanel against the current
    /// hypothesis viewmodel. LateUpdate order 500, after the viewmodel
    /// systems (order 100) and before the input-refresh sweep (order
    /// 1000). State-mutation rules: the system touches only the panel's
    /// Unity-side visuals via HypothesisPanelVisualUtility.Apply.
    /// </summary>
    public class HypothesisPanelVisualSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 500, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchHypothesisPagesState>()
                    .ReadShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchPools>()
                    .ReadWrite<ResearchHypothesisPanel>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState viewModel,
                out ResearchPools pools
            );

            foreach (var panel in Find.Components<ResearchHypothesisPanel>()) {
                HypothesisPanelVisualUtility.Apply(panel, pagesState, viewModel, pools);
            }
        }
    }

    /// <summary>
    /// Pushes viewmodel state into a ResearchHypothesisPanel's
    /// inspector-assigned visuals and into the shared dot pool. Mutation
    /// is on Unity components owned by the panel + dot instances (text,
    /// GameObject active, transform position); the shared state
    /// arguments are read-only.
    /// </summary>
    public static class HypothesisPanelVisualUtility {
        public static void Apply(ResearchHypothesisPanel panel, ResearchHypothesisPagesState pagesState, HypothesisViewModelState viewModel, ResearchPools pools) {
            if (panel == null || pagesState == null || viewModel == null) {
                return;
            }

            int pageCount = pagesState.Pages.Count;
            int activeIdx = viewModel.ActivePageIndex;

            // 1. Pagination dots — grow / shrink ActivePaginationDots
            // to match pageCount via the shared PaginationDotPool, then
            // toggle each dot's ConfirmedOverlay against the viewmodel's
            // per-page fulfilled mask. Newly alloced dots are reparented
            // under the panel's PaginationDotContainer so the panel's
            // layout group can position them. Base sprites are inspector-
            // authored on each dot prefab and always render.
            SyncPaginationDots(pools, panel.PaginationDotContainer, pageCount);
            uint fulfilledMask = viewModel.PageFulfilledMask;
            ResearchPaginationDot activeDot = null;
            if (pools != null && pools.ActivePaginationDots != null) {
                for (int i = 0; i < pools.ActivePaginationDots.Count; i++) {
                    ResearchPaginationDot dot = pools.ActivePaginationDots[i];
                    if (dot == null) continue;
                    if (dot.ConfirmedOverlay != null) {
                        bool confirmed = (fulfilledMask & (1u << i)) != 0;
                        dot.ConfirmedOverlay.enabled = confirmed;
                    }
                    if (i == activeIdx) {
                        activeDot = dot;
                    }
                }
            }

            // 1b. CurrentHypothesisIndicator — move to the active dot's
            // world position and show it; hide when there are no pages.
            if (panel.CurrentHypothesisIndicator != null) {
                bool indicatorVisible = pageCount > 0 && activeDot != null;
                panel.CurrentHypothesisIndicator.gameObject.SetActive(indicatorVisible);
                if (indicatorVisible) {
                    panel.CurrentHypothesisIndicator.position = activeDot.transform.position;
                }
            }

            // 2. Arrow visibility — only when more than one page.
            if (panel.LeftArrow != null) {
                panel.LeftArrow.gameObject.SetActive(pageCount > 1);
            }
            if (panel.RightArrow != null) {
                panel.RightArrow.gameObject.SetActive(pageCount > 1);
            }

            // 3. Empty-page fast path.
            if (pageCount == 0) {
                if (panel.HeaderLabel != null) {
                    panel.HeaderLabel.text = string.Empty;
                }
                ClearChips(panel);
                return;
            }

            // 4. Active page header + chips. Per-page fulfilled state is
            // already on each dot's ConfirmedOverlay above. Submit
            // button visibility lives on the sample panel.
            HypothesisPage page = pagesState.Pages[activeIdx];
            if (panel.HeaderLabel != null) {
                panel.HeaderLabel.text = "FIND A " + MaterialPropertyLabelDisplay.GetPropertyName(page.Label);
            }
            RenderChips(panel, page, viewModel.ActivePageSatisfiedMask, viewModel.ActivePageLockedMask);
        }

        private static void RenderChips(ResearchHypothesisPanel panel, HypothesisPage page, uint satisfiedMask, uint lockedMask) {
            if (panel.Chips == null) {
                return;
            }
            MaterialObservationEntry[] leaves = page.DecomposedObservations;
            int leafCount = leaves != null ? leaves.Length : 0;
            for (int i = 0; i < panel.Chips.Length; i++) {
                if (i >= leafCount) {
                    panel.Chips[i].gameObject.SetActive(false);
                    continue;
                }
                panel.Chips[i].gameObject.SetActive(true);
                bool filled = (satisfiedMask & (1u << i)) != 0;
                bool locked = (lockedMask & (1u << i)) != 0;
                panel.Chips[i].SetState(MaterialPropertyLabelDisplay.GetObservationName(leaves[i].Label), filled, locked, leaves[i].ObservationType);
            }
        }

        private static void ClearChips(ResearchHypothesisPanel panel) {
            if (panel.Chips == null) {
                return;
            }
            for (int i = 0; i < panel.Chips.Length; i++) {
                panel.Chips[i].gameObject.SetActive(false);
            }
        }

        // Grows or shrinks ResearchPools.ActivePaginationDots to match
        // `count` by Alloc/TryFree against PaginationDotPool. Mirrors
        // how VFX instances are alloced from ExplosionEffectPool /
        // BoltZapEffectPool. Newly alloced dots are reparented under
        // `container` (the panel's PaginationDotContainer) so the
        // panel's layout group lays them out; freed dots return to the
        // pool's own root via SerializablePool's default free behavior.
        // No-op if the pools state isn't wired.
        private static void SyncPaginationDots(ResearchPools pools, RectTransform container, int count) {
            if (pools == null || pools.ActivePaginationDots == null || pools.PaginationDotPool == null) {
                return;
            }

            var active = pools.ActivePaginationDots;

            // Grow: alloc new dots until we hit the target count.
            // SetParent(container, false) preserves the prefab's local
            // layout values so the layout group on container takes over
            // cleanly.
            while (active.Count < count) {
                ResearchPaginationDot dot = pools.PaginationDotPool.Alloc();
                if (dot == null) {
                    break;
                }
                if (container != null) {
                    dot.transform.SetParent(container, false);
                }
                active.Add(dot);
            }

            // Shrink: free surplus dots back to the pool.
            while (active.Count > count) {
                int last = active.Count - 1;
                ResearchPaginationDot dot = active[last];
                active.RemoveAt(last);
                if (dot != null) {
                    Pool.TryFree(dot);
                }
            }
        }
    }
}

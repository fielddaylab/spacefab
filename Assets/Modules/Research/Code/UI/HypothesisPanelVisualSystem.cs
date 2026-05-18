using BeauPools;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Renders the singleton ResearchHypothesisPanel against the current
    /// hypothesis viewmodel, but only when work is pending. LateUpdate
    /// order 500, after the viewmodel systems (order 100) and before the
    /// input-refresh sweep (order 1000). Refresh gate: re-applies visuals
    /// only when the panel's NeedsRefresh flag is raised or the viewmodel
    /// reports a change this frame; otherwise early-outs and leaves the
    /// last-applied visuals in place.
    /// </summary>
    public class HypothesisPanelVisualSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 500, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchHypothesisPagesState>()
                    .ReadShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchPools>()
                    .ReadWriteShared<ResearchHypothesisPanelState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchHypothesisPanelState panel,
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState viewModel,
                out ResearchPools pools
            );

            if (panel == null) {
                return;
            }

            // Refresh gate: skip the whole render unless either side has
            // asked for it. NeedsRefresh handles first-paint + ad-hoc
            // raises; HypothesisChangedThisFrame handles viewmodel
            // rebuilds (page cycle, slot change, ObservationCollect /
            // HypothesisSubmit invalidations).
            if (!panel.NeedsRefresh && (viewModel == null || !viewModel.HypothesisChangedThisFrame)) {
                return;
            }

            HypothesisPanelVisualUtility.Apply(panel, pagesState, viewModel, pools);
            panel.NeedsRefresh = false;
        }
    }

    /// <summary>
    /// Pushes viewmodel state into the ResearchHypothesisPanel's
    /// inspector-assigned visuals and into the shared dot pool. Invoked
    /// only when a refresh has been requested (see
    /// HypothesisPanelVisualSystem). Mutation is on Unity components
    /// owned by the panel + dot instances (text, GameObject active,
    /// transform position); the pagesState and viewModel arguments are
    /// read-only.
    /// </summary>
    public static class HypothesisPanelVisualUtility {
        public static void Apply(ResearchHypothesisPanelState panel, ResearchHypothesisPagesState pagesState, HypothesisViewModelState viewModel, ResearchPools pools) {
            if (panel == null || pagesState == null || viewModel == null) {
                return;
            }

            int pageCount = pagesState.Pages.Count;
            int activeIdx = viewModel.ActivePageIndex;

            // 1. Pagination dots — grow / shrink ActivePaginationDots
            // to match pageCount via the shared PaginationDotPool, then
            // lay the active set out horizontally, centered on the
            // container's local X=0, and toggle each dot's
            // ConfirmedOverlay against the viewmodel's per-page fulfilled
            // mask. Base sprites are inspector-authored on each dot
            // prefab and always render.
            SyncPaginationDots(pools, panel.PaginationDotContainer, pageCount);
            LayoutPaginationDots(pools);
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
            // Force last-sibling so the indicator renders on top of the
            // dots regardless of authored hierarchy order (dots are
            // alloced from a pool and reparented in, which can shuffle
            // sibling indices).
            if (panel.CurrentHypothesisIndicator != null) {
                bool indicatorVisible = pageCount > 0 && activeDot != null;
                panel.CurrentHypothesisIndicator.gameObject.SetActive(indicatorVisible);
                if (indicatorVisible) {
                    panel.CurrentHypothesisIndicator.position = activeDot.transform.position;
                    panel.CurrentHypothesisIndicator.SetAsLastSibling();
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

        private static void RenderChips(ResearchHypothesisPanelState panel, HypothesisPage page, uint satisfiedMask, uint lockedMask) {
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
            LayoutChips(panel.Chips, leafCount);
        }

        // Gap (px) between adjacent chip rects. Measured edge-to-edge so
        // chips of differing heights still sit flush at the same visual
        // gap.
        private const float ChipGap = 8f;

        // Lays the first `visibleCount` chips out vertically, centered
        // on the parent transform's local Y=0. Chip 0 sits at the top
        // (reading order). X/Z are left untouched so the chip prefab's
        // authored horizontal alignment stays intact. Heights are read
        // from each RectTransform's rect.height so the layout adapts to
        // prefab changes without a magic constant.
        private static void LayoutChips(ResearchObservationChip[] chips, int visibleCount) {
            if (chips == null || visibleCount <= 0) {
                return;
            }
            if (visibleCount > chips.Length) {
                visibleCount = chips.Length;
            }

            // 1. Sum heights to compute the total column height including gaps.
            float totalHeight = 0f;
            for (int i = 0; i < visibleCount; i++) {
                ResearchObservationChip chip = chips[i];
                if (chip == null) continue;
                RectTransform rect = chip.transform as RectTransform;
                totalHeight += rect != null ? rect.rect.height : 0f;
            }
            totalHeight += ChipGap * (visibleCount - 1);

            // 2. Walk top-to-bottom starting at +totalHeight/2, placing
            // each chip's center at cursor - height/2, then advancing
            // the cursor downward by height + gap.
            float cursor = totalHeight * 0.5f;
            for (int i = 0; i < visibleCount; i++) {
                ResearchObservationChip chip = chips[i];
                if (chip == null) continue;
                RectTransform rect = chip.transform as RectTransform;
                float height = rect != null ? rect.rect.height : 0f;
                if (rect != null) {
                    Vector3 pos = rect.anchoredPosition3D;
                    pos.y = cursor - height * 0.5f;
                    rect.anchoredPosition3D = pos;
                } else {
                    Vector3 pos = chip.transform.localPosition;
                    pos.y = cursor - height * 0.5f;
                    chip.transform.localPosition = pos;
                }
                cursor -= height + ChipGap;
            }
        }

        private static void ClearChips(ResearchHypothesisPanelState panel) {
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
        // `container` (the panel's PaginationDotContainer) so
        // LayoutPaginationDots can position them; freed dots return to
        // the pool's own root via SerializablePool's default free
        // behavior. No-op if the pools state isn't wired.
        private static void SyncPaginationDots(ResearchPools pools, RectTransform container, int count) {
            if (pools == null || pools.ActivePaginationDots == null || pools.PaginationDotPool == null) {
                return;
            }

            var active = pools.ActivePaginationDots;

            // Grow: alloc new dots until we hit the target count.
            // SetParent(container, false) preserves the prefab's local
            // layout values.
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

        // Gap (px) between adjacent dot rects. Measured edge-to-edge,
        // not center-to-center, so dots of differing widths still sit
        // flush at the same visual gap.
        private const float DotGap = 8f;

        // Lays the active dots out horizontally, centered on the
        // container's local X=0. Y/Z are left untouched so the dot
        // prefab's authored vertical alignment stays intact. Dot widths
        // are read from each RectTransform's rect.width so the layout
        // adapts to prefab changes without a magic constant.
        private static void LayoutPaginationDots(ResearchPools pools) {
            if (pools == null || pools.ActivePaginationDots == null) {
                return;
            }
            var active = pools.ActivePaginationDots;
            int count = active.Count;
            if (count == 0) {
                return;
            }

            // 1. Sum widths to compute the total row width including gaps.
            float totalWidth = 0f;
            for (int i = 0; i < count; i++) {
                ResearchPaginationDot dot = active[i];
                if (dot == null) continue;
                RectTransform rect = dot.transform as RectTransform;
                totalWidth += rect != null ? rect.rect.width : 0f;
            }
            totalWidth += DotGap * (count - 1);

            // 2. Walk left-to-right starting at -totalWidth/2, placing
            // each dot's center at cursor + width/2, then advancing the
            // cursor by width + gap.
            float cursor = -totalWidth * 0.5f;
            for (int i = 0; i < count; i++) {
                ResearchPaginationDot dot = active[i];
                if (dot == null) continue;
                RectTransform rect = dot.transform as RectTransform;
                float width = rect != null ? rect.rect.width : 0f;
                if (rect != null) {
                    Vector3 pos = rect.anchoredPosition3D;
                    pos.x = cursor + width * 0.5f;
                    rect.anchoredPosition3D = pos;
                } else {
                    Vector3 pos = dot.transform.localPosition;
                    pos.x = cursor + width * 0.5f;
                    dot.transform.localPosition = pos;
                }
                cursor += width + DotGap;
            }
        }
    }
}

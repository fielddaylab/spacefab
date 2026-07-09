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
            RenderChips(panel, page, viewModel.ActivePageLeafSatisfiedMask, viewModel.ActivePageLeafLockedMask);
        }

        private static void RenderChips(ResearchHypothesisPanelState panel, HypothesisPage page, uint satisfiedMask, uint lockedMask) {
            if (panel.GoalLabels == null) {
                return;
            }
            MaterialObservationEntry[] leaves = page.DecomposedObservations;
            int leafCount = leaves != null ? leaves.Length : 0;
            for (int i = 0; i < panel.GoalLabels.Length; i++) {
                if (i >= leafCount) {
                    panel.GoalLabels[i].gameObject.SetActive(false);
                    continue;
                }
                panel.GoalLabels[i].gameObject.SetActive(true);
                bool filled = (satisfiedMask & (1u << i)) != 0;
                bool locked = (lockedMask & (1u << i)) != 0;
                panel.GoalLabels[i].SetState(MaterialPropertyLabelDisplay.GetObservationName(leaves[i].Label), filled, locked, leaves[i].ObservationType);
            }
            LayoutChips(panel.GoalLabels, leafCount);
        }

        // Gap (px) between adjacent chip rects. Measured edge-to-edge so
        // chips of differing heights still sit flush at the same visual
        // gap.
        private const float ChipGap = 8f;

        // Delegates to ResearchUILayoutUtility so the picker uses the
        // same vertical-centered math. Hypothesis panel doesn't resize
        // its parent, so the returned height is discarded.
        private static void LayoutChips(ResearchObservationChip[] chips, int visibleCount) {
            ResearchUILayoutUtility.LayoutVerticalCentered(chips, visibleCount, ChipGap);
        }

        private static void ClearChips(ResearchHypothesisPanelState panel) {
            if (panel.GoalLabels == null) {
                return;
            }
            for (int i = 0; i < panel.GoalLabels.Length; i++) {
                panel.GoalLabels[i].gameObject.SetActive(false);
            }
        }
    }
}

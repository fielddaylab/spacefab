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
    /// inspector-assigned visuals. Invoked only when a refresh has been
    /// requested (see HypothesisPanelVisualSystem). Mutation is on Unity
    /// components owned by the panel (text, GameObject active, transform
    /// position); the pagesState and viewModel arguments are read-only.
    /// </summary>
    public static class HypothesisPanelVisualUtility {
        public static void Apply(ResearchHypothesisPanelState panel, ResearchHypothesisPagesState pagesState, HypothesisViewModelState viewModel, ResearchPools pools) {
            if (panel == null || pagesState == null || viewModel == null) {
                return;
            }

            int pageCount = pagesState.Pages.Count;
            if (pageCount == 0) {
                ClearChips(panel);
                return;
            }

            RenderChips(panel, pagesState, viewModel.PageFulfilledMask);
        }

        private static void RenderChips(ResearchHypothesisPanelState panel, ResearchHypothesisPagesState pagesState, uint pageFulfilledMask) {            
            if (panel.PropertyChips == null) {
                return;
            }
            int pageCount = pagesState.Pages.Count;
            for (int i = 0; i < panel.PropertyChips.Length; i++)
            {
                if (i >= pageCount)
                {
                    panel.PropertyChips[i].gameObject.SetActive(false);
                    continue;
                }
                HypothesisPage page = pagesState.Pages[i];
                panel.PropertyChips[i].gameObject.SetActive(true);
                bool filled = (pageFulfilledMask & (1u << i)) != 0;
                panel.PropertyChips[i].SetState(MaterialPropertyLabelDisplay.GetPropertyName(page.Label), !filled, false, MaterialObservationChamberLookup.GetChamberType(page.Label));
            }
            
            // TODO: For now, the maximum number of goals in a contract is four, so layout is unnecessary.
            // Uncomment this line if level design changes.
            // LayoutChips(panel.PropertyChips, pageCount);
        }

        // Gap (px) between adjacent chip rects. Measured edge-to-edge so
        // chips of differing heights still sit flush at the same visual
        // gap.
        // private const float ChipGap = 8f;

        // Delegates to ResearchUILayoutUtility so the picker uses the
        // same vertical-centered math. Hypothesis panel doesn't resize
        // its parent, so the returned height is discarded.
        // private static void LayoutChips(ResearchObservationChip[] chips, int visibleCount) {
        //     ResearchUILayoutUtility.LayoutVerticalCentered(chips, visibleCount, ChipGap);
        // }

        private static void ClearChips(ResearchHypothesisPanelState panel) {
            if (panel.PropertyChips == null) {
                return;
            }
            for (int i = 0; i < panel.PropertyChips.Length; i++) {
                panel.PropertyChips[i].gameObject.SetActive(false);
            }
        }
    }
}

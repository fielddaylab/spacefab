using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;

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
                    .ReadWrite<ResearchHypothesisPanel>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState viewModel
            );

            foreach (var panel in Find.Components<ResearchHypothesisPanel>()) {
                HypothesisPanelVisualUtility.Apply(panel, pagesState, viewModel);
            }
        }
    }

    /// <summary>
    /// Pushes viewmodel state into a ResearchHypothesisPanel's
    /// inspector-assigned visuals. All mutation here is on Unity components
    /// the panel owns (text, image color, GameObject active); the shared
    /// state arguments are read-only.
    /// </summary>
    public static class HypothesisPanelVisualUtility {
        public static void Apply(ResearchHypothesisPanel panel, ResearchHypothesisPagesState pagesState, HypothesisViewModelState viewModel) {
            if (panel == null || pagesState == null || viewModel == null) {
                return;
            }

            int pageCount = pagesState.Pages.Count;
            int activeIdx = viewModel.ActivePageIndex;

            // 1. Pagination dots — enable up to pageCount, tint the active.
            if (panel.PaginationDots != null) {
                for (int i = 0; i < panel.PaginationDots.Length; i++) {
                    bool show = i < pageCount;
                    panel.PaginationDots[i].gameObject.SetActive(show);
                    if (show) {
                        panel.PaginationDots[i].color = (i == activeIdx) ? panel.DotActiveColor : panel.DotInactiveColor;
                    }
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
                if (panel.FulfilledCheckmark != null) {
                    panel.FulfilledCheckmark.SetActive(false);
                }
                if (panel.SubmitButton != null) {
                    panel.SubmitButton.gameObject.SetActive(false);
                }
                return;
            }

            // 4. Active page header + chips + fulfilled / submit visibility.
            HypothesisPage page = pagesState.Pages[activeIdx];
            if (panel.HeaderLabel != null) {
                panel.HeaderLabel.text = "FIND A " + MaterialPropertyLabelDisplay.GetPropertyName(page.Label);
            }
            RenderChips(panel, page, viewModel.ActivePageSatisfiedMask, viewModel.ActivePageLockedMask);

            if (panel.FulfilledCheckmark != null) {
                panel.FulfilledCheckmark.SetActive(viewModel.ActivePageIsFulfilled);
            }
            if (panel.SubmitButton != null) {
                panel.SubmitButton.gameObject.SetActive(viewModel.SubmitButtonVisible);
            }
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
    }
}

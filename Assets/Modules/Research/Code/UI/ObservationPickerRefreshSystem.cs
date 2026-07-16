using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Materials;

namespace SpaceFab.Research {
    /// <summary>
    /// Updates each active picker chip's disabled state when the
    /// hypothesis viewmodel changes. Gated on
    /// HypothesisViewModelState.HypothesisChangedThisFrame so the per-
    /// chip pass only runs on the same frame an observation was added /
    /// removed / the page cycled (the same trigger
    /// HypothesisPanelVisualSystem uses). Runs on LateUpdate order 600
    /// — after the viewmodel rebuild (100) and the hypothesis-panel
    /// render (500), before the input-refresh sweep (1000) clears the
    /// frame-flag. ResearchMask, not ResearchChamberMask: disabled
    /// refresh is UI-scoped, not chamber-scoped.
    ///
    /// A picker chip is disabled when either (a) its label is already
    /// in the viewmodel's slot view, or (b) the slot view is full
    /// (SlotCount &gt;= ObservationCount). Both conditions can only
    /// change on a viewmodel rebuild, so the gating flag is sufficient.
    /// </summary>
    public class ObservationPickerRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 600, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchPools>()
                    .ReadWrite<ResearchSamplePanel>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out HypothesisViewModelState viewModel,
                out ResearchPools pools
            );

            if (viewModel == null || !viewModel.HypothesisChangedThisFrame) return;
            if (pools == null || pools.ActivePickerChips == null) return;

            bool slotsFull = viewModel.ActivePageSlotCount >= HypothesisViewModelState.MaxObservationsPerPage;

            foreach (var panel in Find.Components<ResearchSamplePanel>()) {
                if (panel == null || panel.PickerLabels == null) continue;
                int n = pools.ActivePickerChips.Count;
                for (int i = 0; i < n; i++) {
                    ResearchObservationChip chip = pools.ActivePickerChips[i];
                    if (chip == null || i >= panel.PickerLabels.Count) continue;
                    bool disabled = slotsFull || IsLabelInSlots(viewModel, panel.PickerLabels[i]);
                    chip.SetPickerChipDisabledVisual(disabled);
                }
            }
        }

        // True if the picker label (Context == Null for chamber-emitted
        // observations) appears in the viewmodel's slot view (auto-locked
        // or player-picked).
        private static bool IsLabelInSlots(HypothesisViewModelState viewModel, MaterialPropertyLabel label) {
            int count = viewModel.ActivePageSlotCount;
            for (int i = 0; i < count; i++) {
                if (viewModel.ActivePageSlotLabels[i] == label && viewModel.ActivePageSlotContexts[i] == StringHash32.Null) {
                    return true;
                }
            }
            return false;
        }
    }
}

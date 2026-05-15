using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;

namespace SpaceFab.Research {
    /// <summary>
    /// Reads the submit-click frame-flag and routes it through
    /// ResearchInventoryUtility.TryConfirmHypothesis for the active page's
    /// (Label, Context) against the slotted material. The existing utility
    /// handles the evaluator + observation consumption + sandbox bit set.
    /// Multiple definitions per label are fine: the evaluator OR-combines
    /// definitions when picking which one to consume, so the page-chosen
    /// definition need not match the satisfying one.
    /// </summary>
    public class HypothesisSubmitSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 110, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchUIInputState>()
                    .ReadShared<ResearchHypothesisPagesState>()
                    .ReadWriteShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchMinigameState>()
                    .ReadShared<ChamberInterfacerState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchUIInputState inputState,
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState viewModelState,
                out ResearchMinigameState researchState
            );

            if (!inputState.SubmitHypothesisClickedThisFrame) {
                return;
            }

            Find.State(out ChamberInterfacerState interfacerState);

            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset slotted = primarySlot != null ? primarySlot.CurrentMaterial : null;
            if (slotted == null) {
                return;
            }

            int pageIndex = viewModelState.ActivePageIndex;
            if (pageIndex < 0 || pageIndex >= pagesState.Pages.Count) {
                return;
            }

            HypothesisPage page = pagesState.Pages[pageIndex];
            if (ResearchInventoryUtility.TryConfirmHypothesis(researchState, slotted.AssetId, page.Label, page.Context)) {
                // A new property bit flipped (or the property was already
                // confirmed and the call was idempotent). Either way the
                // viewmodel's IsFulfilled / SatisfiedMask depends on the
                // record state — request a rebuild so the visual updates
                // next LateUpdate.
                HypothesisViewModelUtility.RequestRebuild(viewModelState);
            }
        }
    }
}

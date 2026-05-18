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
    ///
    /// Runs on LateUpdate at order 60 — after Unity's EventSystem has
    /// dispatched the submit click (CursorHint.onClick fires there) and
    /// after ObservationCollectSystem (order 50) so any chip the player
    /// added the same frame is already in the inventory, and before
    /// HypothesisViewModelSystem at order 100 so the confirmation bit
    /// shows up in the same-frame rebuild. ResearchUIInputRefreshSystem
    /// at order 1000 clears the flag last.
    /// </summary>
    public class HypothesisSubmitSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 60, UpdateMasks.ResearchMask),
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
            } else {
                // Verification failed. Strip any slot whose (label,
                // context) doesn't match a leaf on this page — the
                // player picked the wrong observations. Locked slots
                // are ancestor-confirmed (not in researchState.Observations)
                // and can't be removed; they remain regardless.
                if (PruneIncorrectPicks(researchState, slotted.AssetId, page, viewModelState)) {
                    HypothesisViewModelUtility.RequestRebuild(viewModelState);
                }
            }
        }

        // For each non-locked slot whose (label, context) isn't a leaf
        // of `page`, remove it from researchState.Observations. Returns
        // true if any removal happened.
        private static bool PruneIncorrectPicks(ResearchMinigameState researchState, StringHash32 materialId, HypothesisPage page, HypothesisViewModelState viewModelState) {
            int slotCount = viewModelState.ActivePageSlotCount;
            MaterialObservationEntry[] leaves = page.DecomposedObservations;
            int leafCount = leaves != null ? leaves.Length : 0;
            bool anyRemoved = false;

            for (int i = 0; i < slotCount; i++) {
                bool locked = (viewModelState.ActivePageSlotLockedMask & (1u << i)) != 0;
                if (locked) continue;

                MaterialPropertyLabel slotLabel = viewModelState.ActivePageSlotLabels[i];
                StringHash32 slotContext = viewModelState.ActivePageSlotContexts[i];
                if (LeafMatches(leaves, leafCount, slotLabel, slotContext)) continue;

                if (ResearchInventoryUtility.RemoveObservation(researchState, materialId, slotLabel, slotContext)) {
                    anyRemoved = true;
                }
            }
            return anyRemoved;
        }

        // True if some leaf matches (label, context).
        private static bool LeafMatches(MaterialObservationEntry[] leaves, int leafCount, MaterialPropertyLabel label, StringHash32 context) {
            for (int i = 0; i < leafCount; i++) {
                if (leaves[i].Label == label && leaves[i].Context == context) {
                    return true;
                }
            }
            return false;
        }
    }
}

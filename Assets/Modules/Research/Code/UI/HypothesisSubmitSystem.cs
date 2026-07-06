using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Design;
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
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchUIInputState inputState,
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState viewModelState,
                out ResearchMinigameState researchState
            );

            Find.State(
                out PlayerProgressState progressState,
                out ContractState contractState
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

            // Pre-validate every non-locked pick against (a) the
            // hypothesis page's leaves and (b) the slotted material's
            // ground-truth Properties array. Any pick that fails either
            // check gets pruned now; the hypothesis is rejected (no
            // confirm) if any pruning happens. This keeps Silicon +
            // Conductive from confirming "Conductor" just because the
            // observation evaluator's logic is satisfied — Conductive
            // simply isn't true for Silicon.
            
            string failureReason = null;
            bool anyPruned = PruneIncorrectPicks(researchState, slotted, page, viewModelState, out failureReason);
            if (anyPruned) {
                HypothesisViewModelUtility.RequestRebuild(viewModelState);

                using (var table = TempVarTable.Alloc()) {
                    table.Set("result", "failure");
                    table.Set("reason", failureReason ?? "observation_incorrect");
                    ScriptUtility.Trigger(ResearchScriptTriggers.OnHypothesisSubmitted, table);
                }
                return;
            }

            bool success = ResearchInventoryUtility.TryConfirmHypothesis(researchState, progressState, contractState, slotted.AssetId, page.Label, page.Context);
            if (success) {
                // A new property bit flipped (or the property was already
                // confirmed and the call was idempotent). Either way the
                // viewmodel's IsFulfilled / SatisfiedMask depends on the
                // record state — request a rebuild so the visual updates
                // next LateUpdate.
                HypothesisViewModelUtility.RequestRebuild(viewModelState);
            }

            using (var table = TempVarTable.Alloc()) {
                var resultStr = success ? "success" : "failure";
                table.Set("result", resultStr);
                if (!success) {
                    table.Set("reason", failureReason ?? "hypothesis_mismatch");
                }
                ScriptUtility.Trigger(ResearchScriptTriggers.OnHypothesisSubmitted, table);
            }
        }

        // For each non-locked slot, prune it if the (label, context) is
        // either not on the active page's leaves OR not actually true
        // for the slotted material. "Actually true" means the
        // observation appears in the decomposition of some persistent
        // property in MaterialAsset.Properties — see
        // MaterialPropertyDefinitionUtility.IsObservationTrueForProperties.
        // Returns true if any removal happened. Locked slots are
        // ancestor-confirmed (not in researchState.Observations) and
        // can't be removed; they remain regardless.
        private static bool PruneIncorrectPicks(ResearchMinigameState researchState, MaterialAsset material, HypothesisPage page, HypothesisViewModelState viewModelState, out string failureReason) {
            int slotCount = viewModelState.ActivePageSlotCount;
            MaterialObservationEntry[] leaves = page.DecomposedObservations;
            int leafCount = leaves != null ? leaves.Length : 0;
            MaterialPropertyLabel[] trueProperties = material.Properties;
            bool anyRemoved = false;
            string foundReason = null;

            for (int i = 0; i < slotCount; i++) {
                bool locked = (viewModelState.ActivePageSlotLockedMask & (1u << i)) != 0;
                if (locked) continue;

                MaterialPropertyLabel slotLabel = viewModelState.ActivePageSlotLabels[i];
                StringHash32 slotContext = viewModelState.ActivePageSlotContexts[i];

                bool onLeaf = LeafMatches(leaves, leafCount, slotLabel, slotContext);
                bool materialHasIt = MaterialPropertyDefinitionUtility.IsObservationTrueForProperties(trueProperties, slotLabel, slotContext);
                if (onLeaf && materialHasIt) continue;

                // Determine if removal was hypothesis mismatch error or observation error
                if (!onLeaf && materialHasIt) {
                    if (foundReason == null) foundReason = "hypothesis_mismatch";
                } else {
                    if (foundReason == null) foundReason = "observation_incorrect";
                }

                if (ResearchInventoryUtility.RemoveObservation(researchState, material.AssetId, slotLabel, slotContext)) {
                    anyRemoved = true;
                }
            }
            failureReason = foundReason;
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

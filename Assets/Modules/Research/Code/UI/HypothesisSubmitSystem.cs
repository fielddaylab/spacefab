using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Design;
using SpaceFab.Materials;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Reads the submit-click frame-flag and routes it through
    /// ResearchInventoryUtility.TryConfirmHypothesis for the selected
    /// hypothesis's (Label, Context) against the slotted material. The
    /// existing utility handles the evaluator + observation consumption +
    /// sandbox bit set. Multiple definitions per label are fine: the
    /// pre-validation prunes against the union of every registered
    /// definition's leaves, and the evaluator OR-combines definitions
    /// when picking which one to consume.
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
                    .ReadWriteShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchMinigameState>()
                    .ReadShared<ChamberInterfacerState>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchUIInputState inputState,
                out HypothesisViewModelState viewModelState,
                out ResearchMinigameState researchState
            );

            Find.State(
                out PlayerProgressState progressState,
                out ContractState contractState
                );

            if (!inputState.VerifyHypothesisClickedThisFrame) {
                return;
            }

            Find.State(out ChamberInterfacerState interfacerState);

            ResearchSlot slot = interfacerState.ActiveChamber == ActiveChamberKind.Doping ?
                interfacerState.SecondarySlot : interfacerState.PrimarySlot;
            MaterialAsset slotted = slot != null ? slot.CurrentMaterial : null;
            if (slotted == null) {
                return;
            }

            if (!viewModelState.HypothesisSelected) {
                return;
            }

            // Pre-validate every non-locked pick against (a) the
            // hypothesis's decomposed leaves and (b) the slotted
            // material's ground-truth Properties array. Any pick that
            // fails either check gets pruned now; the hypothesis is
            // rejected (no confirm) if any pruning happens. This keeps
            // Silicon + Conductive from confirming "Conductor" just
            // because the observation evaluator's logic is satisfied —
            // Conductive simply isn't true for Silicon.

            bool anyPruned = PruneIncorrectPicks(researchState, slotted, viewModelState, out string failureReason);
            if (anyPruned) {
                HypothesisViewModelUtility.RequestRebuild(viewModelState);

                using (var table = TempVarTable.Alloc()) {
                    table.Set("result", "failure");
                    table.Set("reason", failureReason ?? "observation_incorrect");
                    ScriptUtility.Trigger(ResearchScriptTriggers.OnHypothesisSubmitted, table);
                }
                return;
            }

            bool success = ResearchInventoryUtility.TryConfirmHypothesis(researchState, progressState, contractState, slotted.AssetId, viewModelState.HypothesisLabel, viewModelState.HypothesisContext);
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
        // either not on the hypothesis's decomposed leaves OR not
        // actually true for the slotted material. "Actually true" means
        // the observation appears in the decomposition of some persistent
        // property in MaterialAsset.Properties — see
        // MaterialPropertyDefinitionUtility.IsObservationTrueForProperties.
        // Leaves are the union across every registered definition for the
        // selected label, so a pick supporting any alternate satisfaction
        // path survives. Returns true if any removal happened. Locked
        // slots are ancestor-confirmed (not in researchState.Observations)
        // and can't be removed; they remain regardless.
        private static bool PruneIncorrectPicks(ResearchMinigameState researchState, MaterialAsset material, HypothesisViewModelState viewModelState, out string failureReason) {
            int slotCount = viewModelState.SlotCount;
            List<MaterialObservationEntry> leaves = DecomposeAllDefinitions(viewModelState.HypothesisLabel);
            int leafCount = leaves.Count;
            MaterialPropertyLabel[] trueProperties = material.Properties;
            bool anyRemoved = false;
            string foundReason = null;

            for (int i = 0; i < slotCount; i++) {
                bool locked = (viewModelState.SlotLockedMask & (1u << i)) != 0;
                if (locked) continue;

                MaterialPropertyLabel slotLabel = viewModelState.SlotLabels[i];
                StringHash32 slotContext = viewModelState.SlotContexts[i];

                bool isDopant = MaterialObservationChamberLookup.GetChamberType(slotLabel) == ObservationType.Dopant;
                StringHash32[] contextIds = isDopant ? new StringHash32[material.Contexts.Length] : new StringHash32[] {StringHash32.Null};
                if (isDopant)
                {
                    for (int c = 0; c < material.Contexts.Length; c++)
                    {
                        contextIds[c] = material.Contexts[c].AssetId;
                    }
                }

                bool onLeaf = LeafMatches(leaves, leafCount, slotLabel, null);
                bool materialHasIt = MaterialPropertyDefinitionUtility.IsObservationTrueForProperties(trueProperties, slotLabel, slotContext, contextIds);
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
        private static bool LeafMatches(List<MaterialObservationEntry> leaves, int leafCount, MaterialPropertyLabel label, StringHash32 context) {
            for (int i = 0; i < leafCount; i++) {
                if (leaves[i].Label == label && leaves[i].Context == context) {
                    return true;
                }
            }
            return false;
        }

        // Scratch for the union decomposition; submits are rare and
        // single-threaded, so shared buffers suffice.
        private static readonly List<MaterialObservationEntry> s_UnionScratch = new List<MaterialObservationEntry>(8);
        private static readonly List<MaterialObservationEntry> s_DefScratch = new List<MaterialObservationEntry>(8);
        private static readonly StringHash32[] s_NullContext = new StringHash32[] { StringHash32.Null };

        // Decomposes every registered definition for the label and
        // returns the deduped (label, context) union of their leaves.
        private static List<MaterialObservationEntry> DecomposeAllDefinitions(MaterialPropertyLabel label) {
            s_UnionScratch.Clear();
            MaterialPropertyDefinitionAsset registry = Find.GlobalAsset<MaterialPropertyDefinitionAsset>();
            if (registry == null) {
                return s_UnionScratch;
            }
            MaterialPropertyDefinition[] defs = registry.GetDefinitions(label);
            for (int d = 0; d < defs.Length; d++) {
                s_DefScratch.Clear();
                MaterialPropertyDefinitionUtility.DecomposeToObservations(defs[d], s_NullContext, s_DefScratch);
                for (int i = 0; i < s_DefScratch.Count; i++) {
                    if (!LeafMatches(s_UnionScratch, s_UnionScratch.Count, s_DefScratch[i].Label, s_DefScratch[i].Context)) {
                        s_UnionScratch.Add(s_DefScratch[i]);
                    }
                }
            }
            return s_UnionScratch;
        }
    }
}

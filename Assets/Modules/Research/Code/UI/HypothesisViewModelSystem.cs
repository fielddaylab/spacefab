using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;

namespace SpaceFab.Research {
    /// <summary>
    /// Recomputes HypothesisViewModelState only when something has
    /// requested a rebuild. Rebuild triggers, in order of cost to check:
    ///   - HypothesisCycleDelta != 0 (paginator arrow click this frame)
    ///   - ChamberInterfacerState.SlotMaterialUpdatedThisFrame for the
    ///     Primary slot (the slotted material changed)
    ///   - HypothesisViewModelState.NeedsRebuild (an explicit raise from
    ///     ObservationCollectSystem, HypothesisSubmitSystem, or
    ///     ResearchHypothesisUtility.BuildPages)
    /// When none are set, the system early-returns without touching the
    /// viewmodel — its visual consumers continue to read the same state
    /// that was last computed.
    ///
    /// On rebuild builds two parallel views for the active page:
    ///
    /// * Leaf view (LeafSatisfiedMask / LeafLockedMask): bit L =
    ///   leaf L is satisfied / auto-confirmed. Drives the hypothesis
    ///   panel (which is leaf-indexed: shows the hypothesis's required
    ///   observations).
    ///
    /// * Slot view (SlotLabels / SlotContexts / SlotCount /
    ///   SlotLockedMask): the ordered list of observations occupying
    ///   the sample panel's N slots. Auto-locked entries (ancestor-
    ///   confirmed) come first; player picks follow in
    ///   researchState.Observations insertion order. Player picks may
    ///   include observations that don't match any leaf — they still
    ///   take a slot.
    ///
    /// PageFulfilledMask drives the paginator's per-dot confirmed
    /// overlay. SubmitButtonVisible is true when every leaf is satisfied.
    /// </summary>
    public class HypothesisViewModelSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchHypothesisPagesState>()
                    .ReadWriteShared<HypothesisViewModelState>()
                    .ReadWriteShared<ResearchUIInputState>()
                    .ReadShared<ChamberInterfacerState>()
                    .ReadShared<ResearchMinigameState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState viewModelState,
                out ResearchUIInputState inputState,
                out ChamberInterfacerState interfacerState
            );

            // Rebuild gating. If no trigger fired this frame, leave the
            // viewmodel as-is and clear the per-frame "changed" signal so
            // visual consumers can short-circuit. Slot changes only count
            // when the Primary slot moved — Secondary updates are for
            // dual-slot chambers (Combiner, future) and don't affect the
            // hypothesis viewmodel.
            bool slotChanged = interfacerState.SlotMaterialUpdatedThisFrame
                && interfacerState.LastUpdatedKind == ChamberSlotKind.Primary;
            if (!viewModelState.NeedsRebuild && !slotChanged) {
                viewModelState.HypothesisChangedThisFrame = false;
                return;
            }
            viewModelState.NeedsRebuild = false;

            Find.State(
                out ResearchMinigameState researchState,
                out PlayerProgressState progressState
            );

            int pageCount = pagesState.Pages.Count;
            int prevIndex = viewModelState.ActivePageIndex;
            uint prevLeafSatisfied = viewModelState.ActivePageLeafSatisfiedMask;
            uint prevLeafLocked = viewModelState.ActivePageLeafLockedMask;
            uint prevSlotLocked = viewModelState.ActivePageSlotLockedMask;
            int prevSlotCount = viewModelState.ActivePageSlotCount;
            bool prevSubmit = viewModelState.SubmitButtonVisible;
            uint prevFulfilledMask = viewModelState.PageFulfilledMask;

            // 1. Apply page cycle. Wrapping in both directions; bail to 0
            // when the page list is empty.
            if (pageCount == 0) {
                viewModelState.ActivePageIndex = 0;
                viewModelState.ActivePageObservationCount = 0;
                viewModelState.ActivePageLeafSatisfiedMask = 0;
                viewModelState.ActivePageLeafLockedMask = 0;
                viewModelState.ActivePageLeafSatisfiedCount = 0;
                viewModelState.ActivePageSlotCount = 0;
                viewModelState.ActivePageSlotLockedMask = 0;
                viewModelState.PageFulfilledMask = 0;
                viewModelState.SubmitButtonVisible = false;
                viewModelState.HypothesisChangedThisFrame =
                    prevIndex != 0 || prevLeafSatisfied != 0 || prevSubmit || prevFulfilledMask != 0
                    || prevSlotCount != 0 || prevSlotLocked != 0;
                return;
            }

            // 2. Resolve slotted material + the active page's leaves.
            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset slottedMaterial = primarySlot != null ? primarySlot.CurrentMaterial : null;
            StringHash32 slottedId = slottedMaterial != null ? slottedMaterial.AssetId : StringHash32.Null;

            HypothesisPage page = pagesState.Pages[viewModelState.ActivePageIndex];
            MaterialObservationEntry[] leaves = page.DecomposedObservations;
            int leafCount = leaves != null ? leaves.Length : 0;
            int slotCap = HypothesisViewModelState.MaxObservationsPerPage;
            if (leafCount > slotCap) {
                // Mask is a uint; chips beyond bit 31 silently fall off.
                leafCount = slotCap;
            }
            viewModelState.ActivePageObservationCount = leafCount;

            // 3. Build the slot view. Auto-locked entries first (one per
            // ancestor-confirmed leaf), then player picks in insertion
            // order. Cap at leafCount.
            int slotCount = 0;
            uint slotLockedMask = 0;
            MaterialPropertyLabel[] slotLabels = viewModelState.ActivePageSlotLabels;
            StringHash32[] slotContexts = viewModelState.ActivePageSlotContexts;

            // Lookup the slotted material's confirmed record once.
            MaterialPropertyRecord slottedRecord = default;
            if (slottedMaterial != null) {
                if (!researchState.SandboxProperties.TryGetValue(slottedId, out slottedRecord)) {
                    progressState.MaterialProperties.TryGetValue(slottedId, out slottedRecord);
                }
            }

            // 3a. Auto-locked entries.
            //
            // Two paths auto-populate:
            //   (1) The page's own property is already confirmed for the
            //       slotted material — i.e., the player previously
            //       confirmed this hypothesis (or it was confirmed via
            //       another path). Every leaf is "known" and locked.
            //   (2) The leaf's AncestorProperty is confirmed — a
            //       sub-property already proven for this material
            //       implies the leaf is satisfied.
            // Path (1) takes precedence: if the whole hypothesis is
            // confirmed, every leaf is locked regardless of ancestor
            // state.
            bool pageConfirmed = slottedMaterial != null
                && MaterialPropertyRecordUtility.Has(slottedRecord, page.Label, page.Context);
            for (int i = 0; i < leafCount && slotCount < leafCount; i++) {
                MaterialObservationEntry leaf = leaves[i];
                if (slottedMaterial == null) {
                    continue;
                }
                bool locked = pageConfirmed
                    || (leaf.HasAncestorProperty
                        && MaterialPropertyRecordUtility.Has(slottedRecord, leaf.AncestorProperty, leaf.Context));
                if (!locked) {
                    continue;
                }
                slotLabels[slotCount] = leaf.Label;
                slotContexts[slotCount] = leaf.Context;
                slotLockedMask |= 1u << slotCount;
                slotCount++;
            }

            // 3b. Push player-picked entries from researchState.Observations
            // in insertion order, skipping any duplicate (label, context)
            // already in the slot buffer.
            if (slottedMaterial != null && slotCount < leafCount
                && researchState.Observations.TryGetValue(slottedId, out var pickedList)) {
                int pickedCount = pickedList.Count;
                for (int p = 0; p < pickedCount && slotCount < leafCount; p++) {
                    MaterialPropertyLabel label = MaterialObservationListUtility.GetLabel(pickedList, p);
                    StringHash32 context = MaterialObservationListUtility.GetContext(pickedList, p);
                    if (ContainsSlot(slotLabels, slotContexts, slotCount, label, context)) {
                        continue;
                    }
                    slotLabels[slotCount] = label;
                    slotContexts[slotCount] = context;
                    slotCount++;
                }
            }

            viewModelState.ActivePageSlotCount = slotCount;
            viewModelState.ActivePageSlotLockedMask = slotLockedMask;

            // 4. Build the leaf view. For each leaf L, leaf is satisfied
            // iff some slot entry [0..slotCount) has matching (label,
            // context). Locked = satisfied AND the matching slot is
            // locked (i.e., ancestor-confirmed for this leaf's
            // AncestorProperty).
            uint leafSatisfiedMask = 0;
            uint leafLockedMask = 0;
            int leafSatisfiedCount = 0;
            for (int L = 0; L < leafCount; L++) {
                MaterialObservationEntry leaf = leaves[L];
                int slotIdx = FindSlot(slotLabels, slotContexts, slotCount, leaf.Label, leaf.Context);
                if (slotIdx < 0) {
                    continue;
                }
                leafSatisfiedMask |= 1u << L;
                leafSatisfiedCount++;
                if ((slotLockedMask & (1u << slotIdx)) != 0) {
                    leafLockedMask |= 1u << L;
                }
            }

            viewModelState.ActivePageLeafSatisfiedMask = leafSatisfiedMask;
            viewModelState.ActivePageLeafLockedMask = leafLockedMask;
            viewModelState.ActivePageLeafSatisfiedCount = leafSatisfiedCount;
            // Submit shows whenever the slot view is full, regardless
            // of correctness — the submit handler validates against
            // leaves and removes any slot whose (label, context) doesn't
            // match. The hypothesis is only confirmed if every slot
            // also matches; otherwise the wrong picks get culled and
            // the player tries again.
            viewModelState.SubmitButtonVisible = leafCount > 0 && slotCount == leafCount;

            // 5. Per-page fulfilled mask: bit i = page i has been
            // confirmed by some known material (sandbox or saved
            // PlayerProgress). Drives the paginator's per-dot
            // confirmed-overlay state.
            uint pageFulfilledMask = 0;
            int maskBound = pageCount > 32 ? 32 : pageCount;
            for (int p = 0; p < maskBound; p++) {
                HypothesisPage pageEntry = pagesState.Pages[p];
                if (AnyMaterialFulfills(researchState, progressState, pageEntry.Label, pageEntry.Context)) {
                    pageFulfilledMask |= 1u << p;
                }
            }
            viewModelState.PageFulfilledMask = pageFulfilledMask;

            // 6. Frame-flag — any change drives the panel's LateUpdate render.
            bool changed = viewModelState.ActivePageIndex != prevIndex
                || leafSatisfiedMask != prevLeafSatisfied
                || leafLockedMask != prevLeafLocked
                || slotCount != prevSlotCount
                || slotLockedMask != prevSlotLocked
                || viewModelState.SubmitButtonVisible != prevSubmit
                || pageFulfilledMask != prevFulfilledMask;
            viewModelState.HypothesisChangedThisFrame = changed;
        }

        // Returns true if any of slotLabels[0..count) matches (label, context).
        private static bool ContainsSlot(MaterialPropertyLabel[] slotLabels, StringHash32[] slotContexts, int count, MaterialPropertyLabel label, StringHash32 context) {
            for (int i = 0; i < count; i++) {
                if (slotLabels[i] == label && slotContexts[i] == context) {
                    return true;
                }
            }
            return false;
        }

        // Returns index of the slot in [0..count) that matches (label,
        // context), or -1 if not present.
        private static int FindSlot(MaterialPropertyLabel[] slotLabels, StringHash32[] slotContexts, int count, MaterialPropertyLabel label, StringHash32 context) {
            for (int i = 0; i < count; i++) {
                if (slotLabels[i] == label && slotContexts[i] == context) {
                    return i;
                }
            }
            return -1;
        }

        // True if any material in either the in-session sandbox or the
        // saved PlayerProgress has the property confirmed.
        private static bool AnyMaterialFulfills(ResearchMinigameState researchState, PlayerProgressState progressState, MaterialPropertyLabel label, StringHash32 context) {
            foreach (var kvp in researchState.SandboxProperties) {
                if (MaterialPropertyRecordUtility.Has(kvp.Value, label, context)) {
                    return true;
                }
            }
            foreach (var kvp in progressState.MaterialProperties) {
                if (MaterialPropertyRecordUtility.Has(kvp.Value, label, context)) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Mutators paired with HypothesisViewModelState. Other systems call
    /// RequestRebuild when their work invalidates the viewmodel — e.g.
    /// ObservationCollectSystem after a successful add/remove,
    /// HypothesisSubmitSystem after a successful confirmation, or
    /// ResearchHypothesisUtility.BuildPages on minigame entry.
    /// HypothesisViewModelSystem clears the flag once it has recomputed.
    /// Cycle-delta and ChamberInterfacerState.SlotMaterialUpdatedThisFrame
    /// already imply rebuild and do not need an explicit call.
    /// </summary>
    public static class HypothesisViewModelUtility {
        public static void RequestRebuild(HypothesisViewModelState viewModelState) {
            if (viewModelState == null) return;
            viewModelState.NeedsRebuild = true;
        }
    }
}

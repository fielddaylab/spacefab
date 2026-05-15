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
    /// On rebuild, for the active page, builds:
    ///   - SatisfiedMask: bit i set if leaf i is satisfied (player has
    ///     collected the observation, or the leaf's outermost ancestor
    ///     sub-property is confirmed for the slotted material).
    ///   - LockedMask: subset of SatisfiedMask where satisfaction came
    ///     from the ancestor path (the sample panel renders these as
    ///     immutable).
    ///   - IsFulfilled: true when any material the player knows of has
    ///     the page's (Label, Context) confirmed.
    ///   - SubmitButtonVisible: every leaf satisfied.
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
            bool cycleRequested = inputState.HypothesisCycleDelta != 0;
            if (!viewModelState.NeedsRebuild && !slotChanged && !cycleRequested) {
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
            uint prevSatisfied = viewModelState.ActivePageSatisfiedMask;
            uint prevLocked = viewModelState.ActivePageLockedMask;
            bool prevSubmit = viewModelState.SubmitButtonVisible;
            uint prevFulfilledMask = viewModelState.PageFulfilledMask;

            // 1. Apply page cycle. Wrapping in both directions; bail to 0
            // when the page list is empty.
            if (pageCount == 0) {
                viewModelState.ActivePageIndex = 0;
                viewModelState.ActivePageObservationCount = 0;
                viewModelState.ActivePageSatisfiedMask = 0;
                viewModelState.ActivePageLockedMask = 0;
                viewModelState.ActivePageSatisfiedCount = 0;
                viewModelState.PageFulfilledMask = 0;
                viewModelState.SubmitButtonVisible = false;
                viewModelState.HypothesisChangedThisFrame = prevIndex != 0 || prevSatisfied != 0 || prevSubmit || prevFulfilledMask != 0;
                return;
            }

            int delta = inputState.HypothesisCycleDelta;
            if (delta != 0) {
                int next = ((viewModelState.ActivePageIndex + delta) % pageCount + pageCount) % pageCount;
                viewModelState.ActivePageIndex = next;
            }

            // 2. Resolve slotted material + the active page's leaves.
            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset slottedMaterial = primarySlot != null ? primarySlot.CurrentMaterial : null;
            StringHash32 slottedId = slottedMaterial != null ? slottedMaterial.AssetId : StringHash32.Null;

            HypothesisPage page = pagesState.Pages[viewModelState.ActivePageIndex];
            MaterialObservationEntry[] leaves = page.DecomposedObservations;
            int leafCount = leaves != null ? leaves.Length : 0;
            if (leafCount > 32) {
                // The mask is a uint; chips beyond bit 31 silently fall off.
                leafCount = 32;
            }
            viewModelState.ActivePageObservationCount = leafCount;

            // 3. Build the per-leaf satisfied + locked masks. A leaf is
            // satisfied if either (a) the player has the observation in
            // the slotted material's list, or (b) the leaf's outermost
            // ancestor sub-property is already confirmed for the slotted
            // material (auto-population). Locked is the (b)-only subset.
            uint satisfiedMask = 0;
            uint lockedMask = 0;
            int satisfiedCount = 0;

            // Lookup the slotted material's confirmed record once.
            MaterialPropertyRecord slottedRecord = default;
            if (slottedMaterial != null) {
                if (!researchState.SandboxProperties.TryGetValue(slottedId, out slottedRecord)) {
                    progressState.MaterialProperties.TryGetValue(slottedId, out slottedRecord);
                }
            }

            for (int i = 0; i < leafCount; i++) {
                MaterialObservationEntry leaf = leaves[i];
                bool ancestorConfirmed = false;
                if (leaf.HasAncestorProperty && slottedMaterial != null) {
                    ancestorConfirmed = MaterialPropertyRecordUtility.Has(slottedRecord, leaf.AncestorProperty, leaf.Context);
                }
                bool playerCollected = slottedMaterial != null
                    && ResearchInventoryUtility.HasObservation(researchState, slottedId, leaf.Label, leaf.Context);

                if (ancestorConfirmed || playerCollected) {
                    satisfiedMask |= 1u << i;
                    satisfiedCount++;
                    if (ancestorConfirmed && !playerCollected) {
                        lockedMask |= 1u << i;
                    }
                }
            }

            viewModelState.ActivePageSatisfiedMask = satisfiedMask;
            viewModelState.ActivePageLockedMask = lockedMask;
            viewModelState.ActivePageSatisfiedCount = satisfiedCount;
            viewModelState.SubmitButtonVisible = leafCount > 0 && satisfiedCount == leafCount;

            // 4. Per-page fulfilled mask: bit i = page i has been
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

            // 5. Frame-flag — any change drives the panel's LateUpdate render.
            bool changed = viewModelState.ActivePageIndex != prevIndex
                || satisfiedMask != prevSatisfied
                || lockedMask != prevLocked
                || viewModelState.SubmitButtonVisible != prevSubmit
                || pageFulfilledMask != prevFulfilledMask;
            viewModelState.HypothesisChangedThisFrame = changed;
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

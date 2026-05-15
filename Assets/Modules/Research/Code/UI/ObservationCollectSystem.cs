using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;

namespace SpaceFab.Research {
    /// <summary>
    /// Handles the player's add/remove observation gestures. Reads the
    /// slotted material via ChamberInterfacerState.PrimarySlot. Add comes
    /// from the chip picker's selection emit; remove comes from clicking a
    /// non-locked filled slot in the observations panel. Both fan out into
    /// the existing ResearchInventoryUtility add/remove helpers.
    /// </summary>
    public class ObservationCollectSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 50, UpdateMasks.ResearchMask),
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
                out ResearchMinigameState researchState,
                out ChamberInterfacerState interfacerState
            );

            if (!inputState.ChipPickerSelectedThisFrame && !inputState.RemoveObservationClickedThisFrame) {
                return;
            }

            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset slotted = primarySlot != null ? primarySlot.CurrentMaterial : null;
            if (slotted == null) {
                return;
            }
            StringHash32 slottedId = slotted.AssetId;

            bool viewModelDirty = false;

            // Add path: chip picker selection. Battery scope — context is
            // always null. Combiner will pass a meaningful context once it
            // lands.
            if (inputState.ChipPickerSelectedThisFrame) {
                if (ResearchInventoryUtility.AddObservation(researchState, slottedId, inputState.ChipPickerSelectionLabel, StringHash32.Null)) {
                    viewModelDirty = true;
                }
            }

            // Remove path: slot index → (label, context) via the active
            // page's decomposition. Locked slots are filtered out client-
            // side by ResearchSamplePanel before the click fires; the
            // server-side guard here is the slot-index range check.
            if (inputState.RemoveObservationClickedThisFrame) {
                Find.State(
                    out ResearchHypothesisPagesState pagesState,
                    out HypothesisViewModelState viewModelState
                );

                int idx = inputState.RemoveObservationSlotIndex;
                int pageIndex = viewModelState.ActivePageIndex;
                if (pageIndex >= 0 && pageIndex < pagesState.Pages.Count) {
                    var leaves = pagesState.Pages[pageIndex].DecomposedObservations;
                    if (leaves != null && idx >= 0 && idx < leaves.Length) {
                        MaterialObservationEntry leaf = leaves[idx];
                        if (ResearchInventoryUtility.RemoveObservation(researchState, slottedId, leaf.Label, leaf.Context)) {
                            viewModelDirty = true;
                        }
                    }
                }
            }

            if (viewModelDirty) {
                HypothesisViewModelUtility.RequestRebuild(Find.State<HypothesisViewModelState>());
            }
        }
    }
}

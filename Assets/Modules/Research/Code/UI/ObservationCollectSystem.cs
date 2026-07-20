using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Handles the player's add/remove observation gestures. Reads the
    /// slotted material via ChamberInterfacerState.PrimarySlot. Add comes
    /// from the chip picker's selection emit; remove comes from clicking a
    /// non-locked filled slot in the observations panel. Both fan out into
    /// the existing ResearchInventoryUtility add/remove helpers.
    ///
    /// Runs on LateUpdate at order 50 so it sits *after* Unity's
    /// EventSystem has dispatched any same-frame click (CursorHint's
    /// onClick fires during the EventSystem update, which on this
    /// project lands after FieldDay's Update phase but before LateUpdate)
    /// and *before* HypothesisViewModelSystem at order 100 — so a click
    /// added this frame is visible in the viewmodel rebuild that same
    /// frame. ResearchUIInputRefreshSystem at order 1000 clears the
    /// flags after every consumer has had a chance to read them.
    /// </summary>
    public class ObservationCollectSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 50, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchUIInputState>()
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

            if (!inputState.ChipPickerSelectedThisFrame && !inputState.RemoveObservationClickedThisFrame
                && !inputState.HypothesisSelectedClickedThisFrame && !inputState.RemoveHypothesisClickedThisFrame) {
                return;
            }

            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset slotted = primarySlot != null ? primarySlot.CurrentMaterial : null;
            if (slotted == null) {
                return;
            }
            StringHash32 slottedId = slotted.AssetId;

            // Get the secondary material; null check is done later because it
            // is only used for doping chamber
            bool isDopingChamber = interfacerState.ActiveChamber == ActiveChamberKind.Doping;
            ResearchSlot secondarySlot = interfacerState.SecondarySlot;
            MaterialAsset secondarySlotted = secondarySlot != null ? secondarySlot.CurrentMaterial : null;
            StringHash32 secondarySlottedId = secondarySlotted == null ? null : secondarySlotted.AssetId;

            bool viewModelDirty = false;

            // Add path: chip picker selection. Battery scope — context is
            // always null. Combiner will pass a meaningful context once it
            // lands.
            if (inputState.ChipPickerSelectedThisFrame) {
                if (isDopingChamber)
                {
                    if (secondarySlotted == null) {
                        return;
                    }

                    if (ResearchInventoryUtility.AddObservation(researchState, secondarySlottedId, inputState.ChipPickerSelectionLabel, slottedId)) {
                        viewModelDirty = true;
                        ScriptUtility.Trigger(ResearchScriptTriggers.OnObservationAdded);
                    }
                }
                else if (ResearchInventoryUtility.AddObservation(researchState, slottedId, inputState.ChipPickerSelectionLabel, StringHash32.Null)) {
                    viewModelDirty = true;
                    ScriptUtility.Trigger(ResearchScriptTriggers.OnObservationAdded);
                }
            }

            // Remove path: slot index → (label, context) via the
            // viewmodel's slot buffer. Slot buffer entries may differ
            // from the active page's leaves (player picks not matching
            // any leaf still occupy slots). Locked slots are filtered
            // client-side by SamplePanelInputUtility before the click
            // fires; the server-side guard here is the slot-index range
            // check + the locked-mask test.
            if (inputState.RemoveObservationClickedThisFrame) {
                HypothesisViewModelState viewModelState = Find.State<HypothesisViewModelState>();

                int idx = inputState.RemoveObservationSlotIndex;
                if (viewModelState != null && idx >= 0 && idx < viewModelState.ActivePageSlotCount) {
                    bool locked = (viewModelState.ActivePageSlotLockedMask & (1u << idx)) != 0;
                    if (!locked) {
                        MaterialPropertyLabel label = viewModelState.ActivePageSlotLabels[idx];
                        StringHash32 context = viewModelState.ActivePageSlotContexts[idx];
                        if (isDopingChamber)
                        {
                            if (ResearchInventoryUtility.RemoveObservation(researchState, secondarySlottedId, label, context)) {
                                viewModelDirty = true;
                            }
                        }
                        else if (ResearchInventoryUtility.RemoveObservation(researchState, slottedId, label, context)) {
                            viewModelDirty = true;
                        }
                    }
                }
            }

            // Add hypothesis chip
            if (inputState.HypothesisSelectedClickedThisFrame) {
                HypothesisViewModelState viewModelState = Find.State<HypothesisViewModelState>();
                if (viewModelState != null) {
                    bool locked = (viewModelState.PageFulfilledMask & (1u << viewModelState.ActivePageIndex)) != 0;
                    if (!locked) {
                        viewModelDirty = true;
                    }
                }
            }

            // Remove hypothesis chip
            if (inputState.RemoveHypothesisClickedThisFrame) {
                HypothesisViewModelState viewModelState = Find.State<HypothesisViewModelState>();
                if (viewModelState != null) {
                    viewModelState.ActivePageIndex = -1;
                    viewModelDirty = true;
                }
            }

            if (viewModelDirty) {
                HypothesisViewModelUtility.RequestRebuild(Find.State<HypothesisViewModelState>());
            }
        }
    }
}

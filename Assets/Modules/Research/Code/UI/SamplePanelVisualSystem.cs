using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Renders every active ResearchSamplePanel against the current
    /// hypothesis viewmodel + the slotted material. LateUpdate order 500,
    /// paired with HypothesisPanelVisualSystem. The panel's slot chips
    /// read from the hypothesis viewmodel (single source of truth — both
    /// panels share the active page's leaves); the SAMPLE header is
    /// derived live from the slotted material's ResearchMaterialView; the
    /// picker pulls from BatteryChamberState.AvailableObservations.
    /// </summary>
    public class SamplePanelVisualSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 500, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ChamberInterfacerState>()
                    .ReadShared<ResearchHypothesisPagesState>()
                    .ReadShared<HypothesisViewModelState>()
                    //.ReadShared<BatteryChamberState>()
                    .ReadShared<ResearchMinigameState>()
                    .ReadWrite<ResearchSamplePanel>()
            );
        }

        // Tracks the verify (submit) button's visibility across frames so we can fire
        // OnVerifyButtonShown one frame after it appears — see the dispatch note below.
        private static bool s_PrevSubmitVisible;
        private static bool s_VerifyShownPending;

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ChamberInterfacerState interfacerState,
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState hypoVm
            );
            ResearchMinigameState researchState = Find.State<ResearchMinigameState>();

            foreach (var panel in Find.Components<ResearchSamplePanel>()) {
                SamplePanelVisualUtility.Apply(panel, interfacerState, pagesState, hypoVm, researchState);
            }

            // Onboarding hook: fire OnVerifyButtonShown the frame AFTER the verify button becomes
            // visible. Apply() above activates the button this frame; its ElementTag only registers
            // with the lookup on the next registration pass, so a highlight fired now would miss it.
            // The one-frame defer (arm this frame, dispatch next) guarantees the tag is resolvable.
            if (s_VerifyShownPending) {
                s_VerifyShownPending = false;
                ScriptUtility.Trigger(ResearchScriptTriggers.OnVerifyButtonShown);
            }
            if (hypoVm.VerifyButtonVisible && !s_PrevSubmitVisible) {
                s_VerifyShownPending = true;
            }
            s_PrevSubmitVisible = hypoVm.VerifyButtonVisible;
        }
    }

    /// <summary>
    /// Pushes derived sample state into a ResearchSamplePanel's visuals.
    /// Reads the slotted material directly off ChamberInterfacerState +
    /// the per-material ResearchMaterialView (for SampleNumber). No
    /// intermediate viewmodel; the cheap lookup beats a one-frame-stale
    /// copy. Battery scope: the picker pulls from BatteryChamberState —
    /// extend via a switch on ChamberInterfacerState.ActiveChamber when
    /// other chambers come online.
    /// </summary>
    public static class SamplePanelVisualUtility {
        public static void Apply(
            ResearchSamplePanel panel,
            ChamberInterfacerState interfacerState,
            ResearchHypothesisPagesState pagesState,
            HypothesisViewModelState hypoVm,
            ResearchMinigameState researchState
        ) {
            if (panel == null || interfacerState == null || pagesState == null || hypoVm == null) {
                return;
            }

            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset primaryMaterial = primarySlot != null ? primarySlot.CurrentMaterial : null;

            ResearchSlot secondarySlot = interfacerState.SecondarySlot;
            MaterialAsset secondaryMaterial = secondarySlot != null ? secondarySlot.CurrentMaterial : null;

            // Submit button mirrors the hypothesis viewmodel's
            // SubmitButtonVisible flag (true only when the slotted
            // material satisfies every leaf on the active page).
            // Driven explicitly here so it works in both the empty-state
            // and filled-state paths regardless of where the button
            // sits in the panel hierarchy.
            if (panel.VerifyButton != null) {
                panel.VerifyButton.gameObject.SetActive(hypoVm.VerifyButtonVisible);
            }

            if (interfacerState.ActiveChamberChangedThisFrame)
            {
                ResearchUIAssets uiAssets = Find.GlobalAsset<ResearchUIAssets>();
                ActiveChamberKind chamberKind = ChamberInterfacerUtility.GetActiveChamber(interfacerState);

                if (panel.VoltageChamberButton != null) {
                    panel.VoltageChamberButton.Image.sprite = chamberKind == ActiveChamberKind.Voltage
                        ? uiAssets.VoltagePressed : uiAssets.VoltageNormal;
                }
                if (panel.ThermalChamberButton != null) {
                    panel.ThermalChamberButton.Image.sprite = chamberKind == ActiveChamberKind.Thermal
                        ? uiAssets.ThermalPressed : uiAssets.ThermalNormal;
                }
                if (panel.DopingChamberButton != null) {
                    panel.DopingChamberButton.Image.sprite = chamberKind == ActiveChamberKind.Doping
                        ? uiAssets.DopingPressed : uiAssets.DopingNormal;
                }

                if (panel.ChamberText != null) {
                    panel.ChamberText.text = chamberKind == ActiveChamberKind.None ? "" : $"{chamberKind} Chamber";
                }
            }

            // 1. Empty-state path: no material slotted
            bool isDopingChamber = interfacerState.ActiveChamber == ActiveChamberKind.Doping;
            bool isSlotFilled = isDopingChamber ?
                secondaryMaterial != null : primaryMaterial != null;

            if (!isSlotFilled) {
                if (panel.EmptyState != null) {
                    panel.EmptyState.SetActive(true);
                }
                if (panel.MainContent != null) {
                    panel.MainContent.SetActive(false);
                }
                // Explicitly hide the slot chips even if MainContent's
                // hierarchy doesn't parent them — otherwise stale
                // filled chips from the previously-slotted material
                // remain visible after the sample is pulled.
                if (panel.SlotChips != null) {
                    for (int i = 0; i < panel.SlotChips.Length; i++) {
                        if (panel.SlotChips[i] != null) {
                            panel.SlotChips[i].gameObject.SetActive(false);
                        }
                    }
                }
                if (panel.HypothesisChip != null)
                    panel.HypothesisChip.gameObject.SetActive(false);
                SamplePanelInputUtility.ClosePicker(panel);
                return;
            }

            if (panel.EmptyState != null) {
                panel.EmptyState.SetActive(false);
            }
            if (panel.MainContent != null) {
                panel.MainContent.SetActive(true);
                panel.DopingGroup.SetActive(isDopingChamber);
            }

            // 2. Sample label — derived from the slotted material's view.
            if (panel.SampleLabel != null) {
                // Known materials (any property confirmed in the sandbox)
                // show their ShortName; unknown materials show their
                // sample number prefixed with "SAMPLE ".
                MaterialAsset targetMaterial = isDopingChamber ? secondaryMaterial : primaryMaterial;
                
                bool known = researchState != null
                    && researchState.SandboxProperties.TryGetValue(targetMaterial.AssetId, out var record)
                    && !MaterialPropertyRecordUtility.IsEmpty(record);

                ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(targetMaterial.AssetId);
                panel.SampleSprite.sprite = targetMaterial.GemSprite;
                if (known) {
                    panel.SampleLabel.text = targetMaterial.ShortName;
                } else {
                    //int sampleNumber = view != null ? view.SampleNumber : 0;
                    panel.SampleLabel.text = view != null ? view.SampleLabel : "Z"; // z as fallback
                }

                // Set the substrate label and sprite if currently on doping chamber
                if (isDopingChamber)
                {
                    bool substrateKnown = researchState != null
                        && researchState.SandboxProperties.TryGetValue(primaryMaterial.AssetId, out var substrateRecord)
                        && !MaterialPropertyRecordUtility.IsEmpty(substrateRecord);
                    ResearchMaterialView substrateView = Find.NamedAsset<ResearchMaterialView>(primaryMaterial.AssetId);
                    panel.SubstrateSprite.sprite = primaryMaterial.GemSprite;
                    if (substrateKnown) {
                        panel.SubstrateLabel.text = primaryMaterial.ShortName;
                    } else {
                        panel.SubstrateLabel.text = view != null ? substrateView.SampleLabel : "Z"; // z as fallback
                    }
                }
            }

            // 3. Slot chips render the viewmodel's slot view (auto-
            // locked entries first, then player picks in insertion
            // order). Filled slots [0..SlotCount) show the picked
            // label + that label's per-type sprite; remaining slots
            // up to capacity render dashed-empty.
            int slotCount = hypoVm.ActivePageSlotCount;

            if (panel.SlotChips != null) {
                for (int i = 0; i < panel.SlotChips.Length; i++) {
                    panel.SlotChips[i].gameObject.SetActive(true);
                    bool filled = i < slotCount;
                    bool locked = filled && (hypoVm.ActivePageSlotLockedMask & (1u << i)) != 0;
                    string label = null;
                    ObservationType type = default;
                    if (filled) {
                        MaterialPropertyLabel slotLabel = hypoVm.ActivePageSlotLabels[i];
                        label = MaterialPropertyLabelDisplay.GetObservationName(slotLabel);
                        type = MaterialObservationChamberLookup.GetChamberType(slotLabel);
                    }
                    panel.SlotChips[i].SetState(label, filled, locked, type, useEmptyDashedSprite: true);
                }
            }

            // 3-1. Render hypothesis chip slot
            panel.HypothesisChip.gameObject.SetActive(true);
            bool hypoFilled = hypoVm.ActivePageIndex != -1;
            bool hypoLocked = hypoFilled && (hypoVm.PageFulfilledMask & (1u << hypoVm.ActivePageIndex)) != 0;
            string hypoLabel = null;
            ObservationType hypoType = default;
            if (hypoFilled) {
                MaterialPropertyLabel hypo = pagesState.Pages[hypoVm.ActivePageIndex].Label;
                hypoLabel = MaterialPropertyLabelDisplay.GetPropertyName(hypo);
                hypoType = MaterialObservationChamberLookup.GetChamberType(hypo);
            }
            if (hypoVm.HypothesisContext != StringHash32.Null)
            {
                ResearchMaterialView hypoContext = Find.NamedAsset<ResearchMaterialView>(hypoVm.HypothesisContext);
                hypoLabel += " for " + hypoContext.SampleLabel; // TODO: show actual name for known materials
            }
            panel.HypothesisChip.SetState(hypoLabel, hypoFilled, hypoLocked, hypoType);

            // 4. Picker overlay. Population + layout + resize happen
            // once on chamber load (ObservationPickerLoadUtility);
            // disabled-state refresh happens in
            // ObservationPickerRefreshSystem when the viewmodel changes.
            // Here we only mirror the transient PickerOpen flag.
            if (panel.ChipPickerOverlay != null) {
                panel.ChipPickerOverlay.SetActive(panel.PickerOpen);
            }
        }
    }
}

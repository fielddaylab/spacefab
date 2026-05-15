using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using SpaceFab.Materials;

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
                    .ReadShared<BatteryChamberState>()
                    .ReadWrite<ResearchSamplePanel>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ChamberInterfacerState interfacerState,
                out ResearchHypothesisPagesState pagesState,
                out HypothesisViewModelState hypoVm,
                out BatteryChamberState battery
            );

            foreach (var panel in Find.Components<ResearchSamplePanel>()) {
                SamplePanelVisualUtility.Apply(panel, interfacerState, pagesState, hypoVm, battery);
            }
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
            BatteryChamberState battery
        ) {
            if (panel == null || interfacerState == null || pagesState == null || hypoVm == null) {
                return;
            }

            ResearchSlot primarySlot = interfacerState.PrimarySlot;
            MaterialAsset slottedMaterial = primarySlot != null ? primarySlot.CurrentMaterial : null;

            // Submit button mirrors the hypothesis viewmodel's
            // SubmitButtonVisible flag (true only when the slotted
            // material satisfies every leaf on the active page).
            // Driven explicitly here so it works in both the empty-state
            // and filled-state paths regardless of where the button
            // sits in the panel hierarchy.
            if (panel.SubmitButton != null) {
                panel.SubmitButton.gameObject.SetActive(hypoVm.SubmitButtonVisible);
            }

            // 1. Empty-state path: no material slotted.
            if (slottedMaterial == null) {
                if (panel.EmptyState != null) {
                    panel.EmptyState.SetActive(true);
                }
                if (panel.MainContent != null) {
                    panel.MainContent.SetActive(false);
                }
                SamplePanelInputUtility.ClosePicker(panel);
                return;
            }

            if (panel.EmptyState != null) {
                panel.EmptyState.SetActive(false);
            }
            if (panel.MainContent != null) {
                panel.MainContent.SetActive(true);
            }

            // 2. Sample header — derived from the slotted material's view.
            if (panel.SampleHeader != null) {
                ResearchMaterialView view = Find.NamedAsset<ResearchMaterialView>(slottedMaterial.AssetId);
                int sampleNumber = view != null ? view.SampleNumber : 0;
                panel.SampleHeader.text = "SAMPLE " + sampleNumber.ToString();
            }

            // 3. Slot chips mirror the active hypothesis page's leaves
            // and read fill state from the hypothesis viewmodel.
            int pageCount = pagesState.Pages.Count;
            int leafCount = 0;
            HypothesisPage page = default;
            if (pageCount > 0 && hypoVm.ActivePageIndex >= 0 && hypoVm.ActivePageIndex < pageCount) {
                page = pagesState.Pages[hypoVm.ActivePageIndex];
                leafCount = page.DecomposedObservations != null ? page.DecomposedObservations.Length : 0;
            }

            if (panel.SlotChips != null) {
                for (int i = 0; i < panel.SlotChips.Length; i++) {
                    if (i >= leafCount) {
                        panel.SlotChips[i].gameObject.SetActive(false);
                        continue;
                    }
                    panel.SlotChips[i].gameObject.SetActive(true);
                    bool filled = (hypoVm.ActivePageSatisfiedMask & (1u << i)) != 0;
                    bool locked = (hypoVm.ActivePageLockedMask & (1u << i)) != 0;
                    MaterialObservationEntry leaf = page.DecomposedObservations[i];
                    panel.SlotChips[i].SetState(MaterialPropertyLabelDisplay.GetObservationName(leaf.Label), filled, locked, leaf.ObservationType);
                }
            }

            // 4. Picker overlay. Populate only while open; visibility
            // mirrors the panel's transient PickerOpen flag.
            if (panel.PickerOpen) {
                PopulatePicker(panel, battery);
            }
            if (panel.ChipPickerOverlay != null) {
                panel.ChipPickerOverlay.SetActive(panel.PickerOpen);
            }
        }

        // Fills the panel's picker chip pool from the active chamber's
        // AvailableObservations. Battery only today; extend via a switch
        // on ChamberInterfacerState.ActiveChamber when other chambers
        // come online. ObservationType per label comes from the static
        // MaterialObservationChamberLookup so the picker sprites match
        // the slot-side sprites.
        private static void PopulatePicker(ResearchSamplePanel panel, BatteryChamberState battery) {
            MaterialPropertyLabel[] available = battery != null ? battery.AvailableObservations : null;
            int availableCount = available != null ? available.Length : 0;

            if (panel.PickerChips == null || panel.PickerLabels == null) {
                return;
            }

            for (int i = 0; i < panel.PickerChips.Length; i++) {
                if (i >= availableCount) {
                    panel.PickerChips[i].gameObject.SetActive(false);
                    panel.PickerLabels[i] = default;
                    continue;
                }
                panel.PickerChips[i].gameObject.SetActive(true);
                panel.PickerLabels[i] = available[i];
                ObservationType observationType = MaterialObservationChamberLookup.GetChamberType(available[i]);
                panel.PickerChips[i].SetState(MaterialPropertyLabelDisplay.GetObservationName(available[i]), false, false, observationType);
            }
        }
    }
}

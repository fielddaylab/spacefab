using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.UI;
using SpaceFab.Materials;
using System;
using TMPro;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// View component for the bottom Observations / sample panel. Pure
    /// view: inspector-assigned references plus a transient picker-open
    /// flag + per-instance picker label cache (NonSerialized runtime
    /// fields, mirroring the VoltageControl precedent). Click handlers
    /// route through ResearchUIInputUtility and SamplePanelInputUtility.
    /// Per-frame visual render is in SamplePanelVisualSystem.
    /// </summary>
    public class ResearchSamplePanel : BatchedComponent, IRegistrationCallbacks {
        public TMP_Text SampleHeader;
        public GameObject EmptyState;
        public GameObject MainContent;

        public ResearchObservationChip[] SlotChips;

        public CursorHint AddObservationButton;
        public GameObject ChipPickerOverlay;
        public ResearchObservationChip[] PickerChips;

        public CursorHint SubmitButton;

        public GameObject[] ChamberSwitchButtonStubs;

        // Whether the picker overlay is currently open. Per-instance
        // transient state; the visual system reads it to decide whether
        // to populate + show the overlay.
        [NonSerialized] public bool PickerOpen;

        // Parallel to PickerChips: label bound to each picker chip slot,
        // populated by the visual system from the active chamber's
        // AvailableObservations. Click handlers index into this to know
        // which label to emit. Allocated in OnRegister.
        [NonSerialized] public MaterialPropertyLabel[] PickerLabels;

        // Cached delegate references for picker / slot click handlers so
        // OnDeregister can detach precisely (matching what OnRegister
        // attached). Without this, anonymous closures captured by index
        // could not be removed by reference.
        [NonSerialized] private Action[] m_PickerClickHandlers;
        [NonSerialized] private Action[] m_SlotClickHandlers;

        public void OnRegister() {
            if (PickerChips != null) {
                PickerLabels = new MaterialPropertyLabel[PickerChips.Length];
                m_PickerClickHandlers = new Action[PickerChips.Length];
                for (int i = 0; i < PickerChips.Length; i++) {
                    int captured = i;
                    m_PickerClickHandlers[i] = () => HandlePickerChip(captured);
                    if (PickerChips[i] != null && PickerChips[i].Click != null) {
                        PickerChips[i].Click.onClick.Register(m_PickerClickHandlers[i]);
                    }
                }
            }
            if (SlotChips != null) {
                m_SlotClickHandlers = new Action[SlotChips.Length];
                for (int i = 0; i < SlotChips.Length; i++) {
                    int captured = i;
                    m_SlotClickHandlers[i] = () => HandleSlotClick(captured);
                    if (SlotChips[i] != null && SlotChips[i].Click != null) {
                        SlotChips[i].Click.onClick.Register(m_SlotClickHandlers[i]);
                    }
                }
            }
            if (AddObservationButton != null) {
                AddObservationButton.onClick.Register(HandleAddObservation);
            }
            if (SubmitButton != null) {
                SubmitButton.onClick.Register(HandleSubmit);
            }

            SamplePanelInputUtility.ClosePicker(this);
        }

        public void OnDeregister() {
            if (PickerChips != null && m_PickerClickHandlers != null) {
                for (int i = 0; i < PickerChips.Length; i++) {
                    if (PickerChips[i] != null && PickerChips[i].Click != null && m_PickerClickHandlers[i] != null) {
                        PickerChips[i].Click.onClick.Deregister(m_PickerClickHandlers[i]);
                    }
                }
            }
            if (SlotChips != null && m_SlotClickHandlers != null) {
                for (int i = 0; i < SlotChips.Length; i++) {
                    if (SlotChips[i] != null && SlotChips[i].Click != null && m_SlotClickHandlers[i] != null) {
                        SlotChips[i].Click.onClick.Deregister(m_SlotClickHandlers[i]);
                    }
                }
            }
            if (AddObservationButton != null) {
                AddObservationButton.onClick.Deregister(HandleAddObservation);
            }
            if (SubmitButton != null) {
                SubmitButton.onClick.Deregister(HandleSubmit);
            }
        }

        private void HandleAddObservation() {
            ResearchUIInputUtility.RequestAddObservation(Find.State<ResearchUIInputState>());
            SamplePanelInputUtility.OpenPicker(this);
        }

        private void HandlePickerChip(int index) {
            SamplePanelInputUtility.SubmitPickerSelection(this, Find.State<ResearchUIInputState>(), index);
        }

        private void HandleSlotClick(int index) {
            SamplePanelInputUtility.RequestSlotRemove(this, Find.State<ResearchUIInputState>(), Find.State<HypothesisViewModelState>(), index);
        }

        private void HandleSubmit() {
            ResearchUIInputUtility.RequestSubmit(Find.State<ResearchUIInputState>());
        }
    }

    /// <summary>
    /// Mutators paired with ResearchSamplePanel. Open/close picker,
    /// submit a picker selection, request a slot removal — all funnel
    /// here so the panel keeps no logic of its own beyond click dispatch.
    /// </summary>
    public static class SamplePanelInputUtility {
        public static void OpenPicker(ResearchSamplePanel panel) {
            if (panel == null) return;
            panel.PickerOpen = true;
        }

        public static void ClosePicker(ResearchSamplePanel panel) {
            if (panel == null) return;
            panel.PickerOpen = false;
            if (panel.ChipPickerOverlay != null) {
                panel.ChipPickerOverlay.SetActive(false);
            }
        }

        // Picker chip click. Resolves the bound label, requests the add,
        // and closes the picker. Out-of-range index is treated as a
        // close-only.
        public static void SubmitPickerSelection(ResearchSamplePanel panel, ResearchUIInputState inputState, int index) {
            if (panel == null) {
                return;
            }
            if (panel.PickerLabels == null || index < 0 || index >= panel.PickerLabels.Length) {
                ClosePicker(panel);
                return;
            }
            ResearchUIInputUtility.RequestPickerSelection(inputState, panel.PickerLabels[index]);
            ClosePicker(panel);
        }

        // Sample-slot click. Filtered: only filled, non-locked slots can
        // be removed. Locked = auto-populated via a confirmed ancestor
        // sub-property; empty = nothing to remove.
        public static void RequestSlotRemove(ResearchSamplePanel panel, ResearchUIInputState inputState, HypothesisViewModelState viewModel, int index) {
            if (panel == null || viewModel == null) {
                return;
            }
            if (index < 0 || index >= viewModel.ActivePageObservationCount) {
                return;
            }
            uint bit = 1u << index;
            bool filled = (viewModel.ActivePageSatisfiedMask & bit) != 0;
            bool locked = (viewModel.ActivePageLockedMask & bit) != 0;
            if (!filled || locked) {
                return;
            }
            ResearchUIInputUtility.RequestRemoveObservation(inputState, index);
        }
    }
}

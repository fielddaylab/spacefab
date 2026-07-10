using BeauPools;
using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.Scripting;
using FieldDay.UI;
using SpaceFab.Materials;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// View component for the bottom Observations / sample panel. Pure
    /// view: inspector-assigned references plus transient runtime state
    /// (PickerOpen, the pool-mirrored PickerLabels / PickerClickHandlers
    /// lists). Click handlers route through ResearchUIInputUtility and
    /// SamplePanelInputUtility. Per-frame visual render is in
    /// SamplePanelVisualSystem.
    ///
    /// Picker chips are not authored on this panel — they live in the
    /// pool on ResearchPools.PickerChipPool and are sync'd in
    /// ObservationPickerLoadUtility.LoadFor on chamber load. The panel
    /// only owns the PickerChipContainer the alloced chips reparent
    /// under, and the parallel labels + handlers lists.
    /// </summary>
    public class ResearchSamplePanel : BatchedComponent, IRegistrationCallbacks {
        public TMP_Text SampleHeader;
        public GameObject EmptyState;
        public GameObject MainContent;

        public ResearchObservationChip[] SlotChips;
        public ResearchObservationChip HypothesisChip;

        public CursorHint AddObservationButton;
        public GameObject ChipPickerOverlay;

        // Scene-wired RectTransform under ChipPickerOverlay that pool-
        // alloced picker chips are reparented under. The load utility
        // lays chips out here vertically and resizes the overlay to fit.
        public RectTransform PickerChipContainer;

        public CursorHint VerifyButton;

        public GameObject[] ChamberSwitchButtonStubs;

        // Whether the picker overlay is currently open. Per-instance
        // transient state; the visual system reads it to decide whether
        // to show the overlay.
        [NonSerialized] public bool PickerOpen;

        // Parallel to ResearchPools.ActivePickerChips: label bound to
        // each active picker chip, populated by
        // ObservationPickerLoadUtility on chamber load. Click handlers
        // index into this to know which label to emit.
        [NonSerialized] public List<MaterialPropertyLabel> PickerLabels;

        // Captured-index click handlers, parallel to ActivePickerChips.
        // Registered on chip Alloc, deregistered on Free. Lifecycle
        // managed by SamplePanelInputUtility / ObservationPickerLoadUtility.
        [NonSerialized] public List<Action> PickerClickHandlers;

        // Cached delegate references for slot click handlers so
        // OnDeregister can detach precisely.
        [NonSerialized] private Action[] m_SlotClickHandlers;

        public void OnRegister() {
            PickerLabels = new List<MaterialPropertyLabel>(8);
            PickerClickHandlers = new List<Action>(8);

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

            if (HypothesisChip != null) {
                HypothesisChip.Click.onClick.Register(HandlePropertySlotClick);
            }
            
            if (AddObservationButton != null) {
                AddObservationButton.onClick.Register(HandleAddObservation);
            }
            if (VerifyButton != null) {
                VerifyButton.onClick.Register(HandleSubmit);
            }

            SamplePanelInputUtility.ClosePicker(this);
        }

        public void OnDeregister() {
            if (Game.SharedState.Has<ResearchPools>())
            {
                SamplePanelInputUtility.FreeAllPickerChips(this, Find.State<ResearchPools>());
            }

            if (SlotChips != null && m_SlotClickHandlers != null) {
                for (int i = 0; i < SlotChips.Length; i++) {
                    if (SlotChips[i] != null && SlotChips[i].Click != null && m_SlotClickHandlers[i] != null) {
                        SlotChips[i].Click.onClick.Deregister(m_SlotClickHandlers[i]);
                    }
                }
            }
            if (HypothesisChip != null) {
                HypothesisChip.Click.onClick.Deregister(HandlePropertySlotClick);
            }

            if (AddObservationButton != null) {
                AddObservationButton.onClick.Deregister(HandleAddObservation);
            }
            if (VerifyButton != null) {
                VerifyButton.onClick.Deregister(HandleSubmit);
            }
        }

        private void HandleAddObservation() {
            ResearchUIInputUtility.RequestAddObservation(Find.State<ResearchUIInputState>());
            // Toggle: re-clicking the button while the picker is open
            // closes it. Off-click-elsewhere dismissal lives in
            // ObservationPickerOffClickSystem.
            if (PickerOpen) {
                SamplePanelInputUtility.ClosePicker(this);
            } else {
                SamplePanelInputUtility.OpenPicker(this);
            }
        }

        // Picker chip click. Public so ObservationPickerLoadUtility can
        // bind a captured-index closure to each alloced chip's Click.
        public void HandlePickerChip(int index) {
            SamplePanelInputUtility.SubmitPickerSelection(this, Find.State<ResearchUIInputState>(), index);
        }

        private void HandleSlotClick(int index) {
            SamplePanelInputUtility.RequestSlotRemove(this, Find.State<ResearchUIInputState>(), Find.State<HypothesisViewModelState>(), index);
        }

        private void HandlePropertySlotClick()
        {
            SamplePanelInputUtility.RequestHypothesisSlotRemove(this, Find.State<ResearchUIInputState>(), Find.State<HypothesisViewModelState>());
        }

        private void HandleSubmit() {
            ResearchUIInputUtility.RequestSubmit(Find.State<ResearchUIInputState>());
        }
    }

    /// <summary>
    /// Mutators paired with ResearchSamplePanel. Open/close picker,
    /// submit a picker selection, request a slot removal, free all
    /// pool-held picker chips — all funnel here so the panel keeps no
    /// logic of its own beyond click dispatch.
    /// </summary>
    public static class SamplePanelInputUtility {
        public static void OpenPicker(ResearchSamplePanel panel) {
            if (panel == null) return;
            panel.PickerOpen = true;
            ScriptUtility.Trigger(ResearchScriptTriggers.OnObservationPickerOpened);
        }

        public static void ClosePicker(ResearchSamplePanel panel) {
            if (panel == null) return;
            panel.PickerOpen = false;
            if (panel.ChipPickerOverlay != null) {
                panel.ChipPickerOverlay.SetActive(false);
            }
        }

        // Picker chip click. Resolves the bound label, requests the add.
        // Out-of-range index is treated as a
        // close-only.
        public static void SubmitPickerSelection(ResearchSamplePanel panel, ResearchUIInputState inputState, int index) {
            if (panel == null) {
                return;
            }
            if (panel.PickerLabels == null || index < 0 || index >= panel.PickerLabels.Count) {
                ClosePicker(panel);
                return;
            }
            ResearchUIInputUtility.RequestPickerSelection(inputState, panel.PickerLabels[index]);
        }

        // Sample-slot click. Filtered: only filled, non-locked slots in
        // the viewmodel's slot view can be removed. Locked = auto-
        // populated via a confirmed ancestor sub-property; index past
        // SlotCount = empty placeholder.
        public static void RequestSlotRemove(ResearchSamplePanel panel, ResearchUIInputState inputState, HypothesisViewModelState viewModel, int index) {
            if (panel == null || viewModel == null) {
                return;
            }
            if (index < 0 || index >= viewModel.ActivePageSlotCount) {
                return;
            }
            bool locked = (viewModel.ActivePageSlotLockedMask & (1u << index)) != 0;
            if (locked) {
                return;
            }
            ResearchUIInputUtility.RequestRemoveObservation(inputState, index);
        }

        // Hypothesis-slot click. Can be removed if the slot is filled and non-locked.
        public static void RequestHypothesisSlotRemove(ResearchSamplePanel panel, ResearchUIInputState inputState, HypothesisViewModelState viewModel) {
            if (panel == null || viewModel == null) {
                return;
            }
            if (viewModel.ActivePageIndex == -1) {
                return;
            }
            bool locked = (viewModel.PageFulfilledMask & (1u << viewModel.ActivePageIndex)) != 0;
            if (locked) {
                return;
            }
            ResearchUIInputUtility.RequestRemoveHypothesis(inputState);
        }

        // Returns every pool-held picker chip to the pool, deregistering
        // its captured-index click handler in lockstep. Called from
        // ResearchSamplePanel.OnDeregister and as the first step of
        // ObservationPickerLoadUtility.LoadFor (clean slate on chamber
        // load). No-op if pools or its active list is null (mid-tear-
        // down, or pre-Preload).
        public static void FreeAllPickerChips(ResearchSamplePanel panel, ResearchPools pools) {
            if (panel == null || pools == null || pools.ActivePickerChips == null) return;
            int n = pools.ActivePickerChips.Count;
            for (int i = n - 1; i >= 0; i--) {
                ResearchObservationChip chip = pools.ActivePickerChips[i];
                Action handler = panel.PickerClickHandlers != null && i < panel.PickerClickHandlers.Count
                    ? panel.PickerClickHandlers[i]
                    : null;
                if (chip != null && chip.Click != null && handler != null) {
                    chip.Click.onClick.Deregister(handler);
                }
                if (chip != null) {
                    Pool.TryFree(chip);
                }
            }
            pools.ActivePickerChips.Clear();
            if (panel.PickerClickHandlers != null) panel.PickerClickHandlers.Clear();
            if (panel.PickerLabels != null) panel.PickerLabels.Clear();
        }
    }
}

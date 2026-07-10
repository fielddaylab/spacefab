using FieldDay;
using FieldDay.HID;
using FieldDay.SharedState;
using FieldDay.UI;
using System;
using TMPro;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// View for the top hypothesis panel: header, left/right arrows,
    /// observation chips, the pagination dot row, and the moving
    /// current-hypothesis indicator. Singleton — only one panel exists
    /// in scope at a time, so it lives as a SharedStateComponent rather
    /// than a per-entity component.
    ///
    /// HypothesisPanelVisualSystem renders the panel only when work is
    /// pending: either NeedsRefresh has been raised, or the viewmodel
    /// reports HypothesisChangedThisFrame. Click dispatchers here route
    /// through ResearchUIInputUtility and do not mutate visuals directly.
    ///
    /// Pagination dots live in ResearchPools.PaginationDotPool — the
    /// panel does not own them, but it owns PaginationDotContainer (the
    /// RectTransform alloced dots get reparented under) and
    /// CurrentHypothesisIndicator (a single transform the visual util
    /// repositions over the active dot when refreshing). Chip slots are
    /// a fixed pool with hide-unused: max count is bounded by the
    /// largest hypothesis decomposition we expect.
    /// </summary>
    public class ResearchHypothesisPanelState : SharedStateComponent, IRegistrationCallbacks {
        // Goal property labels
        public ResearchObservationChip[] PropertyChips;
        
        // Cached delegate references for slot click handlers so
        // OnDeregister can detach precisely.
        [NonSerialized] private Action[] m_SlotClickHandlers;

        // Refresh request flag. HypothesisPanelVisualSystem reads it
        // alongside HypothesisViewModelState.HypothesisChangedThisFrame
        // to decide whether to re-apply visuals this frame, and clears
        // it after applying. Initialized true in OnRegister so the
        // panel paints once on first activation even when no viewmodel
        // change has been reported yet.
        [NonSerialized] public bool NeedsRefresh;

        public void OnRegister() {
            if (PropertyChips != null) {
                m_SlotClickHandlers = new Action[PropertyChips.Length];
                for (int i = 0; i < PropertyChips.Length; i++) {
                    int captured = i;
                    m_SlotClickHandlers[i] = () => HandlePropertyClick(captured);
                    if (PropertyChips[i] != null && PropertyChips[i].Click != null) {
                        PropertyChips[i].Click.onClick.Register(m_SlotClickHandlers[i]);
                    }
                }
            }

            NeedsRefresh = true;
        }

        public void OnDeregister() {
            if (PropertyChips != null && m_SlotClickHandlers != null) {
                for (int i = 0; i < PropertyChips.Length; i++) {
                    if (PropertyChips[i] != null && PropertyChips[i].Click != null && m_SlotClickHandlers[i] != null) {
                        PropertyChips[i].Click.onClick.Deregister(m_SlotClickHandlers[i]);
                    }
                }
            }
        }

        private void HandlePropertyClick(int index)
        {
            ResearchUIInputUtility.RequestHypothesisSelection(Find.State<ResearchUIInputState>(), index);
        }
    }

    /// <summary>
    /// Mutators paired with ResearchHypothesisPanel. Today this only
    /// raises the visual-refresh flag; the visual system clears it.
    /// </summary>
    public static class ResearchHypothesisPanelUtility {
        public static void RequestRefresh(ResearchHypothesisPanelState panel) {
            if (panel == null) return;
            panel.NeedsRefresh = true;
        }
    }
}

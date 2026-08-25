using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// View for the Contract Requirements list: every research goal on the
    /// active contract shown at once, each row switching to its chip's
    /// confirmed (green, checkmarked) sprite once some known material
    /// satisfies it. Singleton — only one panel exists in scope at a
    /// time, so it lives as a SharedStateComponent rather than a
    /// per-entity component.
    ///
    /// Each row renders through a ResearchObservationChip, the same view
    /// the sample panel and wiki use, so a requirement reads as a chip
    /// like every other property in the game. Rows are a fixed authored
    /// set with hide-unused rather than a pool: a contract carries at
    /// most a handful of goals, so the row count is bounded at authoring
    /// time. Display only — no click handlers are registered on their
    /// CursorHint.
    ///
    /// ContractRequirementsVisualSystem renders the panel only when work
    /// is pending — NeedsRefresh has been raised, or a property was
    /// confirmed this frame.
    /// </summary>
    public class ResearchContractRequirementsPanelState : SharedStateComponent, IRegistrationCallbacks {
        public ResearchContractRequirementRow[] Rows;

        // Refresh request flag. The visual system reads it alongside
        // ResearchMinigameState.PropertyConfirmedThisFrame to decide
        // whether to re-apply visuals, and clears it after applying.
        // Initialized true in OnRegister so the panel paints once on first
        // activation even when nothing has been confirmed yet.
        [NonSerialized] public bool NeedsRefresh;

        public void OnRegister() {
            NeedsRefresh = true;
        }

        public void OnDeregister() {
        }
    }

    /// <summary>
    /// Mutators paired with ResearchContractRequirementsPanelState. Today
    /// this only raises the visual-refresh flag; the visual system clears
    /// it.
    /// </summary>
    public static class ResearchContractRequirementsPanelUtility {
        public static void RequestRefresh(ResearchContractRequirementsPanelState panel) {
            if (panel == null) return;
            panel.NeedsRefresh = true;
        }
    }
}

using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.UI;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Scene-authored references for the Supply mini progress meter: the always-visible
    /// aggregate view, the expandable per-ship section (a CanvasGroup faded by the transition
    /// routine), the authored per-ship rows, and the expand/collapse toggle button. Wires the
    /// toggle in OnRegister and snaps to the collapsed steady state on scene late-init.
    /// </summary>
    public class SupplyProgressMeterLayoutState : SharedStateComponent, ISceneLateInitialize {
        [Header("Aggregate")]
        public SupplyProgressMeterView AggregateView;
        public RectTransform PanelRect;

        public void LateInitialize() {
            // Snap to the collapsed-or-expanded steady state once all states have registered.
            SupplyProgressMeterState state = Find.State<SupplyProgressMeterState>();
            state.NeedsRefresh = true;
        }
    }
}

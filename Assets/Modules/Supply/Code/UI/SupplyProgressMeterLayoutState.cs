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
    public class SupplyProgressMeterLayoutState : SharedStateComponent, IRegistrationCallbacks, ISceneLateInitialize {
        [Header("Aggregate")]
        public SupplyProgressMeterView AggregateView;
        public RectTransform PanelRect;

        [Header("Per-ship breakdown")]
        // Faded in/out by the transition routine to reveal the per-ship rows.
        public CanvasGroup ExpandedSection;
        // One row per ship slot (length up to SupplyRouteData.MaxShips); the utility toggles
        // each active.
        // TODO: pool dynamically based on level need
        public SupplyShipBreakdownRow[] ShipRows;

        public DynamicButton ToggleButton;

        public void OnRegister() {
            // Toggle the expand state and (re)play the transition routine.
            ToggleButton.onClick.AddListener(() => {
                SupplyProgressMeterState state = Find.State<SupplyProgressMeterState>();
                state.Expanded = !state.Expanded;
                state.TransitionRoutine.Replace(SupplyProgressMeterUtility.ToggleRoutine(state, this));
            });
        }

        public void OnDeregister() {
            ToggleButton.onClick.RemoveAllListeners();
        }

        public void LateInitialize() {
            // Snap to the collapsed-or-expanded steady state once all states have registered.
            SupplyProgressMeterState state = Find.State<SupplyProgressMeterState>();
            SupplyProgressMeterUtility.ApplySteadyState(this, state.Expanded);
            state.NeedsRefresh = true;
        }
    }
}

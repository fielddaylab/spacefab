using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;

namespace SpaceFab.Supply {
    /// <summary>
    /// Data for the Supply mini progress meter: the expand/collapse flag, the transition
    /// routine handle, and the cached aggregate signature the visuals system diffs against to
    /// avoid rebuilding every frame. Data only — no logic beyond IRegistrationCallbacks.
    /// </summary>
    public class SupplyProgressMeterState : SharedStateComponent, IRegistrationCallbacks {
        // Forces a rebuild on the next visuals tick regardless of the diff (set on load).
        public bool NeedsRefresh;

        // Expand/collapse steady state and the in-flight transition.
        public bool Expanded;
        public bool Transitioning;
        public Routine TransitionRoutine;

        // Last-applied aggregate values; the visuals system rebuilds only when these change.
        public int LastRisk;
        public int LastCost;
        public int LastTime;
        public int LastActiveMask;

        public void OnRegister() {
            NeedsRefresh = true;
            Expanded = false;
        }

        public void OnDeregister() {
            TransitionRoutine.Stop();
        }
    }
}

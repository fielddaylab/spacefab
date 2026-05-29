using FieldDay;
using Leaf.Runtime;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.StationControl;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Leaf-callable surface for the microgame precision gate and restart, routed through
    /// StationControlState / StationControlUtility. Auto-discovered at boot via MethodCache.LoadStatic.
    /// </summary>
    public static class StationControlScripting {
        // Gates the just-completed microgame's exit on precision. Called from a node responding to the
        // OnFabMicrogameCompleted trigger, passing the per-station threshold. When the cached result
        // precision meets the threshold the microgame exits normally; otherwise it pauses for a restart.
        [LeafMember("RequireMicrogamePrecision")]
        public static void Leaf_RequireMicrogamePrecision(float threshold) {
            if (!Game.SharedState.Has<StationControlState>()) {
                return;
            }
            StationControlState stationState = Find.State<StationControlState>();
            MicrogameExitVerdict verdict = stationState.LastMicrogamePrecision >= threshold
                ? MicrogameExitVerdict.Proceed
                : MicrogameExitVerdict.Retry;
            StationControlUtility.SetCompletionVerdict(stationState, verdict);
        }

        // Restarts the microgame currently paused for retry, resetting it to a fresh play state. Lets a
        // Leaf-driven restart panel trigger the reset; a UI restart button calls the same
        // StationControlUtility.RestartMicrogame.
        [LeafMember("RestartMicrogame")]
        public static void Leaf_RestartMicrogame() {
            if (!Game.SharedState.Has<StationControlState>()) {
                return;
            }
            StationControlUtility.RestartMicrogame(Find.State<StationControlState>());
        }
    }
}

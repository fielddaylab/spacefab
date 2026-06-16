using BeauUtil;
using FieldDay;
using Leaf.Runtime;
using SpaceFab.Overarching;
using SpaceFab.Save;

namespace SpaceFab {
    /// <summary>
    /// Leaf-callable queries and commands that span all minigames (i.e. that route through
    /// MinigameStateInterfacer rather than addressing one specific minigame's systems).
    /// </summary>
    public static class MinigameScripting {
        // True when the current minigame's runtime state has FoundValidSolution set. Returns
        // false when called outside a minigame scene (no interfacer is registered).
        [LeafMember("MinigameHasValidSolution")]
        public static bool Leaf_HasValidSolution() {
            if (!Game.SharedState.Has<MinigameStateInterfacer>()) {
                return false;
            }
            return Find.State<MinigameStateInterfacer>().HasValidSolution();
        }

        // True when the named minigame ("Design", "Research", ...) has a persisted valid solution.
        // Reads the saved per-minigame flag, so it answers "has this been solved" from any scene
        // (e.g. the overarching scene), unlike MinigameHasValidSolution which only sees the active
        // minigame. Returns false for an unknown name or before save states are registered.
        [LeafMember("IsSolutionFoundFor")]
        public static bool Leaf_IsSolutionFoundFor(StringHash32 minigameId) {
            if (!MinigameIdUtility.TryResolve(minigameId, out MinigameId id)) {
                return false;
            }
            if (!Game.SharedState.Has<MinigameSaveStates>()) {
                return false;
            }
            MinigameSaveStateBase save = MinigameSaveUtility.GetState(Find.State<MinigameSaveStates>(), id);
            return save != null && save.FoundValidSolution;
        }
    }
}

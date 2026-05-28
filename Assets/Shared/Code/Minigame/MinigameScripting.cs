using FieldDay;
using Leaf.Runtime;

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
    }
}

using FieldDay;
using Leaf.Runtime;
using SpaceFab.Save;

namespace SpaceFab.Design {
    /// <summary>
    /// Leaf-callable queries and commands specific to the Design minigame.
    /// </summary>
    public static class DesignScripting {
        // Returns the zero-based index of the Design level the player is currently working on
        // within the active contract. Used by onboarding scripts to gate beats on the first level.
        // Returns 0 when no Design minigame is active.
        [LeafMember("CurrLevelIndex")]
        public static int Leaf_CurrLevelIndex() {
            if (!Game.SharedState.Has<DesignMinigameState>()) { return 0; }
            return Find.State<DesignMinigameState>().ActiveLevelIndex;
        }

        // Returns whether the Design level at the given index has been solved. Returns false when
        // no save state is present or the index is out of range for the active contract.
        [LeafMember("IsLevelSolved")]
        public static bool Leaf_IsLevelSolved(int levelIndex) {
            if (!Game.SharedState.Has<MinigameSaveStates>()) { return false; }
            DesignSaveState designSaveState = Find.State<MinigameSaveStates>().Design;
            if (levelIndex < 0 || levelIndex >= designSaveState.LevelCount) { return false; }
            return designSaveState.FoundValidSolutionForLevel[levelIndex];
        }
    }
}

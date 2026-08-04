using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.SharedState;
using Leaf.Runtime;
using SpaceFab.Save;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// One minigame's unlock prerequisites. The minigame is Locked until every entry in
    /// Prerequisites is Complete (FoundValidSolution). Authored on OverarchingAlertState.UnlockRules.
    /// </summary>
    [System.Serializable]
    public struct MinigameUnlockRule
    {
        public MinigameId Minigame;
        // All must be Complete before Minigame unlocks. Empty (or no rule for a minigame at all)
        // means the minigame is always available.
        public MinigameId[] Prerequisites;
    }

    /// <summary>
    /// Stores the list of dependencies per minigame.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Minigame Dependency Graph")]
    public class MinigameDependencyGraph : GlobalAsset
    {
        // Designer-authored unlock prerequisites. A minigame with a rule is Locked until every
        // prerequisite is Complete; a minigame with no rule (or empty Prerequisites) is always
        // available. Expresses arbitrary unlock graphs (e.g. Supply requires Design AND Research).
        public MinigameUnlockRule[] UnlockRules;
    }
}

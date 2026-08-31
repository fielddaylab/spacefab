using BeauUtil;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// Stable identifier for a minigame zone. The first four entries (Design / Research /
    /// Fabrication / Supply) map 1:1 to MinigameSaveStates' fields
    ///
    /// COUNT must stay at the end — it sizes OverarchingAlertState.Masks. When a new minigame
    /// is added, append it before COUNT, then also extend MinigameSaveStates and the switch in
    /// OverarchingAlertUtility.ApplyAutoRuleFromSaveStates.
    /// </summary>
    public enum MinigameId
    {
        Design,
        Research,
        Fabrication,
        Supply,
        COUNT,
    }

    /// <summary>
    /// Query helpers for MinigameId, including name-to-id resolution for Leaf-facing callers
    /// that address a minigame by its enum name (e.g. "Design").
    /// </summary>
    public static class MinigameIdUtility
    {
        // Resolves a minigame's enum name (hashed) to its MinigameId. Returns true and sets id on
        // a match; returns false (id left as COUNT) for an unknown name. Used by Leaf queries that
        // take a minigame by name so script authors aren't coupled to enum ordering.
        public static bool TryResolve(StringHash32 name, out MinigameId id)
        {
            if (name == "Design") {
                id = MinigameId.Design;
                return true;
            }
            if (name == "Research") {
                id = MinigameId.Research;
                return true;
            }
            if (name == "Fabrication") {
                id = MinigameId.Fabrication;
                return true;
            }
            if (name == "Supply") {
                id = MinigameId.Supply;
                return true;
            }

            id = MinigameId.COUNT;
            return false;
        }
    }
}

using FieldDay;
using SpaceFab.Save;

namespace SpaceFab.Design
{
    /// <summary>
    /// Resolves the active Design level for the contract the player is currently working on.
    /// Centralizes the (ContractAssetsWrapper.DesignLevels[ActiveLevelIndex]) lookup so the test
    /// suite, allowed tools, and grid config all read from one place instead of each call site
    /// re-deriving the index.
    /// </summary>
    public static class DesignLevelUtility
    {
        // Returns the LevelData for the player's currently-active Design level, or null if the
        // active contract / its wrapper isn't resolvable or the index is out of range.
        public static LevelData GetActiveLevelData(PlayerProgressState progressState, DesignMinigameState designState)
        {
            ContractAssetsWrapper contractAssets = Find.NamedAsset<ContractAssetsWrapper>(progressState.ContractAssetsWrapperId);
            if (contractAssets == null || contractAssets.DesignLevels == null)
            {
                return null;
            }

            int idx = designState.ActiveLevelIndex;
            if (idx < 0 || idx >= contractAssets.DesignLevels.Length)
            {
                return null;
            }

            return contractAssets.DesignLevels[idx];
        }

        // Marks the active level solved in the save state and mirrors the contract-wide aggregate
        // (all levels solved) onto the runtime flag overarching reads. Save-buffer persistence is
        // the caller's concern (export/Save happens in the exit / continue flow).
        public static void MarkActiveLevelSolved(DesignSaveState saveState, DesignMinigameState designState)
        {
            int idx = designState.ActiveLevelIndex;
            if (saveState.LevelCount <= 0 || idx < 0 || idx >= saveState.LevelCount) { return; }

            saveState.FoundValidSolutionForLevel[idx] = true;
            designState.FoundValidSolution = DesignSaveUtility.AllLevelsSolved(saveState);
        }

        // Clears the active level's solved flag (e.g. on a grid edit) and refreshes the aggregate
        // runtime flag. Once any level becomes unsolved the contract is no longer complete.
        public static void ClearActiveLevelSolved(DesignSaveState saveState, DesignMinigameState designState)
        {
            int idx = designState.ActiveLevelIndex;
            if (saveState.LevelCount <= 0 || idx < 0 || idx >= saveState.LevelCount) { return; }

            saveState.FoundValidSolutionForLevel[idx] = false;
            designState.FoundValidSolution = DesignSaveUtility.AllLevelsSolved(saveState);
        }
    }
}

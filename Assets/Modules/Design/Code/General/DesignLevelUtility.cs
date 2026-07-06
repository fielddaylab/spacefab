using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
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

        // Advances out of the active level: when more levels remain, reloads the Design scene
        // (re-import lands on the next first-unsolved level); on the last level, confirms the normal
        // exit back to overarching. Shared by the results-panel "Continue" flow and the debug skip
        // button so both route through one implementation. Callers are expected to have already
        // marked the active level solved when that's the intent.
        public static void AdvanceFromActiveLevel(DesignSaveState saveState, DesignMinigameState designState, MinigameLoadExitState loadExitState, MinigameRequestExitState requestExitState, MinigameStateInterfacer interfacer)
        {
            int activeIdx = designState.ActiveLevelIndex;
            int levelCount = saveState.LevelCount;

            // Last level (or a degenerate single/zero-level contract): return to overarching, where
            // the now-true aggregate FoundValidSolution lets the contract complete.
            if (activeIdx >= levelCount - 1)
            {
                requestExitState.ExitRequestState = RequestState.Confirmed;
                return;
            }

            // More levels remain: reload the Design scene for the next level. Resolve the scene from
            // the global lookup by the active minigame's id, point the exit pipeline at it, and kick
            // the pipeline. The Exiting phase exports + saves before the reload.
            loadExitState.HasReloadTarget = true;
            loadExitState.ReloadTarget = Game.Scenes.MainScene();

            // Reuse the exit pipeline: flip to Exiting and swap to the transition mask, exactly as
            // MinigameRequestExitSystem does on a confirmed exit.
            loadExitState.Phase = MinigameLoadExitPhase.Exiting;
            GameLoop.SuspendUpdates(Bits.All32);
            GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
        }
    }
}

using FieldDay;
using FieldDay.Data;
using FieldDay.SharedState;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Holds minigame-specific data for the Design minigame.
    /// Central hub for import/export minigame state.
    /// </summary>
    public class DesignMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState, IEditorOnlyData
    {
        #region Saved State

        // public GridStack GridStack; // delegate to GridStackState

        #endregion // Saved State

        #region Session State

        // Switches between the classic per-row "Run" buttons and the new toggle-the-grid-and-Test flow.
        // Session-only; not serialized to save data. Defaults to true (new mode) on each game launch.
        public bool UseToggleInputMode = true;

        // Which Design level under the active contract the player is currently working on. Set on
        // ImportState to the first unsolved level; read by the level-data lookup, the solve-marking
        // logic, and the results "Continue" flow to decide next-level-vs-exit. Session-only.
        [NonSerialized] public int ActiveLevelIndex;

        #endregion // Session State

        [Header("-- DEBUGGING --")]
        public LevelData DebugLevelData;

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.DesignMask | UpdateMasks.SetupMask | UpdateMasks.ToolModeMask | UpdateMasks.WikiMask;
        }

        // IMinigameState

        public override void ImportState(MinigameSaveStates saveStates)
        {
            DesignStateUtility.ImportState(saveStates.Design, this);
        }

        public override void ExportState(ref MinigameSaveStates saveStates)
        {
            DesignStateUtility.ExportState(ref saveStates.Design, this);
        }

#if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorData(bool isDevelopmentBuild) {
            DebugLevelData = null;
        }

#endif // UNITY_EDITOR

#endregion // Interfaces
    }

    public static class DesignStateUtility
    {
        public static void ImportState(DesignSaveState saveState, DesignMinigameState designState)
        {
            // Resume on the first unsolved level; everything below loads that slot's data.
            int idx = DesignSaveUtility.FirstUnsolvedIndex(saveState);
            designState.ActiveLevelIndex = idx;

            if (saveState.LevelCount > 0 && saveState.GridStacks[idx] != null)
            {
                Find.State<GridStackState>().GridStack = saveState.GridStacks[idx];
            }

            // The runtime flag reflects the contract-wide aggregate (all levels solved), which is
            // what overarching's completion check reads through the minigame state.
            designState.FoundValidSolution = DesignSaveUtility.AllLevelsSolved(saveState);

            // InputToggleState may not be registered during very early boot. The seed system will
            // still merge defaults if no saved entries are staged, so a missing state isn't fatal.
            InputToggleState toggleState = Find.State<InputToggleState>();
            if (toggleState != null && saveState.LevelCount > 0)
            {
                InputToggleUtility.ImportFromSaveData(toggleState, saveState.InputToggles[idx]);
            }
        }

        public static void ExportState(ref DesignSaveState saveState, DesignMinigameState designState)
        {
            // Write the live grid/toggles back into the active level's slot only — other levels'
            // saved data is untouched. Guard against an unseeded save (no levels yet).
            if (saveState.LevelCount > 0)
            {
                int idx = designState.ActiveLevelIndex;
                saveState.GridStacks[idx] = Find.State<GridStackState>().GridStack;

                InputToggleState toggleState = Find.State<InputToggleState>();
                if (toggleState != null)
                {
                    InputToggleUtility.ExportToSaveData(toggleState, ref saveState.InputToggles[idx]);
                }
                else
                {
                    saveState.InputToggles[idx].Count = 0;
                }
            }

            // Recompute the contract-wide aggregate from the per-level flags so overarching sees
            // the correct "contract solved" signal on exit.
            saveState.FoundValidSolution = DesignSaveUtility.AllLevelsSolved(saveState);
        }
    }
}
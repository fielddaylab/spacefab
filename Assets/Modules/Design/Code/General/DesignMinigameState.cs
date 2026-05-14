using FieldDay;
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
    public class DesignMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
    {
        #region Saved State

        // public GridStack GridStack; // delegate to GridStackState
        public bool FoundValidSolution;

        #endregion // Saved State

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

        #endregion // Interfaces
    }

    public static class DesignStateUtility
    {
        public static void ImportState(DesignSaveState saveState, DesignMinigameState designState)
        {
            if (saveState.GridStack != null)
            {
                Find.State<GridStackState>().GridStack = saveState.GridStack;
            }

            designState.FoundValidSolution = saveState.FoundValidSolution;
        }

        public static void ExportState(ref DesignSaveState saveState, DesignMinigameState designState)
        {
            saveState.GridStack = Find.State<GridStackState>().GridStack;
            saveState.FoundValidSolution = designState.FoundValidSolution;
        }
    }
}
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research
{
    public class ResearchMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
    {
        #region Saved State

        // TODO: Save State


        #endregion // Saved State

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.ResearchMask;
        }

        // IMinigameState

        public override void ImportState(MinigameSaveStates saveStates)
        {
            ResearchStateUtility.ImportState(saveStates.Research, this);
        }

        public override void ExportState(ref MinigameSaveStates saveStates)
        {
            ResearchStateUtility.ExportState(ref saveStates.Research, this);
        }

        #endregion // Interfaces
    }

    public static class ResearchStateUtility
    {
        public static void ImportState(ResearchSaveState saveState, ResearchMinigameState researchState)
        {
            
        }

        public static void ExportState(ref ResearchSaveState saveState, ResearchMinigameState researchState)
        {

        }
    }
}
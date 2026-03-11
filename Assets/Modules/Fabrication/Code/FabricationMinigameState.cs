using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public class FabricationMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
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
            DefaultUpdateMask = UpdateMasks.FabricationMask;
        }

        // IMinigameState

        public override void ImportState(MinigameSaveStates saveStates)
        {
            FabricationStateUtility.ImportState(saveStates.Fabrication, this);
        }

        public override void ExportState(ref MinigameSaveStates saveStates)
        {
            FabricationStateUtility.ExportState(ref saveStates.Fabrication, this);
        }

        #endregion // Interfaces
    }

    public static class FabricationStateUtility
    {
        public static void ImportState(FabricationSaveState saveState, FabricationMinigameState fabState)
        {
            
        }

        public static void ExportState(ref FabricationSaveState saveState, FabricationMinigameState fabState)
        {

        }
    }
}
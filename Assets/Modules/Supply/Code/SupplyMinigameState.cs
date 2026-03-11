using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply
{
    public class SupplyMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
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
            DefaultUpdateMask = UpdateMasks.SupplyMask;
        }

        // IMinigameState

        public override void ImportState(MinigameSaveStates saveStates)
        {
            SupplyStateUtility.ImportState(saveStates.Supply, this);
        }

        public override void ExportState(ref MinigameSaveStates saveStates)
        {
            SupplyStateUtility.ExportState(ref saveStates.Supply, this);
        }

        #endregion // Interfaces
    }

    public static class SupplyStateUtility
    {
        public static void ImportState(SupplySaveState saveState, SupplyMinigameState supplyState)
        {
            
        }

        public static void ExportState(ref SupplySaveState saveState, SupplyMinigameState supplyState)
        {

        }
    }
}
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Stores minigame-specific data for import/export
    /// </summary>
    public class FabricationMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
    {
        #region Saved State

        [HideInInspector] public int TotalCycles;
        [HideInInspector] public float Precision;

        #endregion // Saved State

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.SetupMask;
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
            fabState.TotalCycles = saveState.TotalCycles;
            fabState.Precision = saveState.Precision;
        }

        public static void ExportState(ref FabricationSaveState saveState, FabricationMinigameState fabState)
        {
            // TODO: check if run completed
            saveState.TotalCycles = fabState.TotalCycles;
            saveState.Precision = fabState.Precision;
        }
    }
}
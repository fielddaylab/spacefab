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

        [HideInInspector] public int Reliability;
        [HideInInspector] public int TotalCycles;
        [HideInInspector] public int Cost;

        // TODO: layout, paths

        #endregion // Saved State

        #region Runtime State

        [HideInInspector] public SupplyChainMapData CurrSupplyChainMap;

        #endregion // Runtime State

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            DefaultUpdateMask = UpdateMasks.SetupMask | UpdateMasks.WikiMask;
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
            supplyState.Reliability = saveState.FinalizedReliability;
            supplyState.TotalCycles = saveState.FinalizedTotalCycles;
            supplyState.Cost = saveState.FinalizedCost;
        }

        public static void ExportState(ref SupplySaveState saveState, SupplyMinigameState supplyState)
        {
            saveState.FinalizedReliability = supplyState.Reliability;
            saveState.FinalizedTotalCycles = supplyState.TotalCycles;
            saveState.FinalizedCost = supplyState.Cost;
        }
    }
}
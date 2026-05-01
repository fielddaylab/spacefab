using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Save;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research
{
    public class ResearchMinigameState : MinigameStateBase, IRegistrationCallbacks, IMinigameState
    {
        #region Saved State

        // TODO: Save State

        #endregion // Saved State

        #region Runtime State

        [HideInInspector] public HashSet<StringHash32> AvailableMaterials = new HashSet<StringHash32>();
        [HideInInspector] public HashSet<StringHash32> RequiredResearchMaterials = new HashSet<StringHash32>();

        #endregion // Runtime State

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
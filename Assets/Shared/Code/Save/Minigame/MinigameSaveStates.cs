using BeauUtil;
using FieldDay;
using FieldDay.Data;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public class MinigameSaveStates : SharedStateComponent, IRegistrationCallbacks
    {
        public DesignSaveState Design;
        public FabricationSaveState Fabrication;
        public ResearchSaveState Research;
        public SupplySaveState Supply;

        #region Interfaces

        // IRegistrationCallbacks

        public void OnDeregister()
        {
            SpacefabGame.SaveBuffer.DeregisterHandler("DesignSaveState");
            SpacefabGame.SaveBuffer.DeregisterHandler("FabricationSaveState");
            SpacefabGame.SaveBuffer.DeregisterHandler("ResearchSaveState");
            SpacefabGame.SaveBuffer.DeregisterHandler("SupplySaveState");
        }

        public void OnRegister()
        {
            MinigameSaveUtility.ClearMinigameState(this);

            SpacefabGame.SaveBuffer.RegisterHandler("DesignSaveState", Design);
            SpacefabGame.SaveBuffer.RegisterHandler("FabricationSaveState", Fabrication);
            SpacefabGame.SaveBuffer.RegisterHandler("ResearchSaveState", Research);
            SpacefabGame.SaveBuffer.RegisterHandler("SupplySaveState", Supply);
        }

        #endregion // Interfaces
    }

    public static class MinigameSaveUtility
    {
        public static void ClearMinigameState(MinigameSaveStates saveStates)
        {
            saveStates.Design = new DesignSaveState();
            saveStates.Design.SetDefaults();

            saveStates.Fabrication = new FabricationSaveState();
            saveStates.Fabrication.SetDefaults();

            saveStates.Research = new ResearchSaveState();
            saveStates.Research.SetDefaults();

            saveStates.Supply = new SupplySaveState();
            saveStates.Supply.SetDefaults();
        }
    }
}
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public interface IMinigameState
    {
        public void ImportState(MinigameSaveStates saveStates);

        public void ExportState(ref MinigameSaveStates saveStates);
    }
}
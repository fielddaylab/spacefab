using FieldDay.Components;
using FieldDay.SharedState;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public abstract class MinigameStateBase : SharedStateComponent, IMinigameState
    {
        [NonSerialized] public int DefaultUpdateMask;

        public bool FoundValidSolution;

        public abstract void ExportState(ref MinigameSaveStates saveStates);

        public abstract void ImportState(MinigameSaveStates saveStates);

        public void MarkFoundValidSolution()
        {
            FoundValidSolution = true;
        }

        public void ClearFoundValidSolution()
        {
            FoundValidSolution = false;
        }
    }
}
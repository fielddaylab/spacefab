using FieldDay.Components;
using SpaceFab.Save;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public abstract class MinigameStateBase : BatchedComponent, IMinigameState
    {
        [NonSerialized] public int DefaultUpdateMask;

        public abstract void ExportState(ref MinigameSaveStates saveStates);

        public abstract void ImportState(MinigameSaveStates saveStates);
    }
}
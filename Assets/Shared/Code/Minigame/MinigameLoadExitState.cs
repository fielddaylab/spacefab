using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public enum MinigameLoadExitPhase
    {
        None,
        Loading,
        Loaded,
        Exiting,
        Exited,
    }

    public class MinigameLoadExitState : SharedStateComponent, IRegistrationCallbacks
    {
        public MinigameLoadExitPhase Phase;

        public void OnRegister()
        {
            // Minigame loads on register
            Phase = MinigameLoadExitPhase.Loading;
        }

        public void OnDeregister()
        {
        }
    }
}
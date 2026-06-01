using BeauUtil;
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
        SavingOnExit,
        Exited,
    }

    public class MinigameLoadExitState : SharedStateComponent, IRegistrationCallbacks
    {
        public MinigameLoadExitPhase Phase;

        // Optional override for the scene loaded at the end of the exit pipeline. When set, the
        // Exited phase loads this scene instead of ReturnMenuState.ReturnScene — used to reload the
        // current minigame for its next level (Design's per-level "Continue") rather than returning
        // to overarching. Cleared after it's consumed so a later real exit returns home as usual.
        public bool HasReloadTarget;
        public SceneReference ReloadTarget;

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
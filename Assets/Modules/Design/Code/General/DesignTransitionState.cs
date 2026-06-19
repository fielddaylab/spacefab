using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.Supply;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public enum DesignTransitionPhase {
        SetupBaseLevel,
        ApplySave,
        FinalizeLevel,
        BuildSimTable,
        SetupComplete,
        Exiting,
    }

    /// <summary>
    /// Holds data facilitating transitioning into and out of the minigame scene.
    /// </summary>
    public class DesignTransitionState : SharedStateComponent, IRegistrationCallbacks
    {
        public DesignTransitionPhase Phase;

        public bool IsLoaded(SceneLoadFence fence) {
            return Phase == DesignTransitionPhase.SetupComplete;
        }

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            Phase = DesignTransitionPhase.SetupBaseLevel;
        }
    }
}
using FieldDay;
using FieldDay.SharedState;
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

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            Phase = DesignTransitionPhase.SetupBaseLevel;
        }
    }
}
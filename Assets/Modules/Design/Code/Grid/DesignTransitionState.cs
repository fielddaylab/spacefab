using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public enum DesignTransitionPhase {
        ApplySave,
        FinalizeLevel,
        SetupComplete,
        Exiting,
    }

    public class DesignTransitionState : SharedStateComponent, IRegistrationCallbacks
    {
        public DesignTransitionPhase Phase;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            Phase = DesignTransitionPhase.ApplySave;
        }
    }
}
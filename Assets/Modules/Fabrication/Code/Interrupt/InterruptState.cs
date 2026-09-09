using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Stations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public class InterruptState : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public bool ResetRequestedThisFrame;
        [NonSerialized] public bool RestoreCheckpointRequestedThisFrame;
        [NonSerialized] public bool FinalizeAttemptRequestedThisFrame;

        public void OnDeregister()
        {

        }

        public void OnRegister()
        {

        }
    }
}

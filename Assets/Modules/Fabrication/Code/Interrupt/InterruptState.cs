using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public class InterruptState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool ResetRequestedThisFrame;
        [HideInInspector] public bool RestoreCheckpointRequestedThisFrame;
        [HideInInspector] public bool FinalizeAttemptRequestedThisFrame;

        public void OnDeregister()
        {

        }

        public void OnRegister()
        {

        }
    }
}

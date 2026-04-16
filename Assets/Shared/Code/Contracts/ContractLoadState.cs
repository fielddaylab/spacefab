using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public enum ContractLoadPhase
    {
        Waiting,
        BeginLoad,
        Loading,
        Completed
    }

    public class ContractLoadState : SharedStateComponent, IRegistrationCallbacks
    {
        public ContractLoadPhase Phase;

        public Routine LoadRoutine;

        public void OnDeregister()
        {
            LoadRoutine.Stop();
        }

        public void OnRegister()
        {
        }
    }
}
using BeauRoutine;
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

    public class ContractLoadState : SharedStateComponent
    {
        public ContractLoadPhase Phase;

        public Routine LoadRoutine;
    }
}
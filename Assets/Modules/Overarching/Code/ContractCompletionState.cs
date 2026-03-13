using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum ContractCompletionPhase
    {
        Waiting,
        Loading,
        EnterPreviousContract,
        EvaluatePreviousContract,
        HidePreviousContract,
        Completed
    }

    public class ContractCompletionState : SharedStateComponent
    {
        public ContractCompletionPhase Phase;
    }
}
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class ContractCompletionState : SharedStateComponent
    {
        public enum ContractCompletionPhase
        {
            Loading, // wait for chapter load
            EnterPreviousContract, // if from end of level
            EvaluatePreviousContract, // if from end of level
            HidePreviousContract, // if from end of level
        }

        public ContractCompletionPhase Phase;
    }
}
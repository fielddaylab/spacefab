using BeauUtil;
using FieldDay.Assets;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpaceFab.Overarching.ContractCompletionState;

namespace SpaceFab.Overarching
{
    public class ContractSelectState : SharedStateComponent
    {
        public enum ContractSelectPhase
        {
            Loading,
            PresentAvailableContracts,
            SelectContract,
            ConfirmContract,
            Completed
        }

        public ContractSelectPhase Phase;
    }
}
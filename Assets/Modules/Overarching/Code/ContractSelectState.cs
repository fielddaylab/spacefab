using BeauUtil;
using FieldDay.Assets;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum ContractSelectPhase
    {
        Waiting,
        Loading,
        PresentAvailableContracts,
        SelectContract,
        ConfirmContract,
        Completed
    }

    public class ContractSelectState : SharedStateComponent
    {
        public ContractSelectPhase Phase;
    }
}
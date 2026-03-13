using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class ContractLayoutState : SharedStateComponent
    {
        public RectTransform FocusedContractZone;
        public RectTransform ContractOptionsZone;

        public ContractOptionButton[] OptionButtons;
    }
}
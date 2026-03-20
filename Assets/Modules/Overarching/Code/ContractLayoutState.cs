using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public class ContractLayoutState : SharedStateComponent, IRegistrationCallbacks
    {
        [Header("Fader")]
        public CanvasGroup FaderGroup;

        [Header("Completion")]
        public CanvasGroup CompletionCanvasGroup;
        public RectTransform CompletedContractZone;
        public ContractUI CompletedContractUI;
        public Vector3 CompletedContractStartPos;

        [Header("Selection")]
        public CanvasGroup SelectionCanvasGroup;
        public RectTransform FocusedContractZone;
        public RectTransform ContractOptionsZone;

        public ContractOptionButton[] OptionButtons;

        public Routine CompletionRoutine;
        public Routine SelectionRoutine;


        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            FaderGroup.alpha = 0;
            CompletionCanvasGroup.alpha = 0;
            SelectionCanvasGroup.alpha = 0;
            CompletedContractUI.gameObject.SetActive(false);
        }
    }
}
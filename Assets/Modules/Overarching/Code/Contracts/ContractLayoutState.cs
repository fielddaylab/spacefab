using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public Vector3 ContractOptionsStartPos;
        public Vector3 ContractOptionsEndPos;
        public Button ConfirmContractButton;

        public ContractOptionButton[] OptionButtons;

        public Routine CompletionRoutine;
        public Routine SelectionRoutine;


        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            FaderGroup.alpha = 0;
            FaderGroup.blocksRaycasts = false;
            CompletionCanvasGroup.alpha = 0;
            SelectionCanvasGroup.alpha = 0;
            CompletedContractUI.gameObject.SetActive(false);

            // Initialize option buttons
            for (int i = 0; i < OptionButtons.Length; i++)
            {
                int tempI = i;
                OptionButtons[i].Button.onClick.AddListener(
                    () =>
                    {
                        Find.State<ContractSelectState>().SelectedContractIndex = tempI;
                    }
                    );
            }

            // Initialize confirm contract button
            ConfirmContractButton.onClick.AddListener(
                    () =>
                    {
                        Find.State<ContractSelectState>().SelectionConfirmed = true;
                    }
                    );
        }
    }
}
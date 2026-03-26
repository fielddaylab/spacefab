using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.UI;
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
        public ContractUI SelectionContractUI;
        public DynamicButton ConfirmContractButton;

        public ContractOptionButton[] OptionButtons;

        [Header("Change")]
        public DynamicButton ViewCurrContractButton;
        public CanvasGroup ChangeCanvasGroup;
        public DynamicButton HideCurrContractButton;
        public DynamicButton ChangeContractButton;
        public DynamicButton CancelChangeContractButton;
        public CanvasGroup DoubleConfirmCanvasGroup;
        public DynamicButton DoubleConfirmContractButton;
        public DynamicButton DoubleCancelContractButton;

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
            ChangeCanvasGroup.alpha = 0;
            DoubleConfirmCanvasGroup.alpha = 0;

            CompletedContractUI.gameObject.SetActive(false);
            ViewCurrContractButton.gameObject.SetActive(false);
            HideCurrContractButton.gameObject.SetActive(false);

            // Initialize option buttons
            for (int i = 0; i < OptionButtons.Length; i++)
            {
                int tempI = i;
                OptionButtons[i].Button.onClick.AddListener(() =>
                    {
                        var selectState = Find.State<ContractSelectState>();
                        if (selectState.SelectedContractIndex != tempI)
                        {
                            selectState.SelectedContractIndex = tempI;
                            selectState.SelectedContractIndexChanged = true;
                        }
                    });
            }

            // Initialize confirm contract button
            ConfirmContractButton.onClick.AddListener(() => {
                    Find.State<ContractSelectState>().SelectionConfirmed = true;
                });

            // Initialize view current contract button
            ViewCurrContractButton.onClick.AddListener(() => {
                    GameLoop.ResumeUpdates(UpdateMasks.ContractSystemsMask);
                    Find.State<ContractChangeState>().Phase = ContractChangePhase.Starting;
                });

            // Initialize change contract button
            ChangeContractButton.onClick.AddListener(() => {
                    Find.State<ContractChangeState>().Phase = ContractChangePhase.ContractSelectSystem;
                });

            // Initialize hide contract button
            HideCurrContractButton.onClick.AddListener(() => {
                    Find.State<ContractChangeState>().Phase = ContractChangePhase.Docking;
                });

            // Initialize cancel change contract button
            CancelChangeContractButton.onClick.AddListener(() => {
                    Find.State<ContractChangeState>().Phase = ContractChangePhase.Viewing;
                });

            // Initialize double confirm contract button
            DoubleConfirmContractButton.onClick.AddListener(() => {
                    Find.State<ContractChangeState>().ChangeDoubleConfirmed = true;
                });

            DoubleCancelContractButton.onClick.AddListener(() => {
                Find.State<ContractChangeState>().Phase = ContractChangePhase.DoubleCancelContract;
            });
        }
    }
}
using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public class DesignRequestExitInterfacerState : SharedStateComponent, IRegistrationCallbacks
    {
        public CanvasGroup ExitConfirmationModal;
        public DynamicButton CancelButton;
        public DynamicButton ConfirmButton;

        public Routine ModalRoutine;

        public void OnDeregister()
        {
            CancelButton.onClick.RemoveAllListeners();
            ConfirmButton.onClick.RemoveAllListeners();

            ModalRoutine.Stop();
        }

        public void OnRegister()
        {
            CancelButton.onClick.AddListener(() =>
            {
                Find.State<MinigameRequestExitState>().ExitRequestState = RequestState.None;
                ModalRoutine.Replace(RequestExitInterfacerUtility.HideExitConfirmationModal(ExitConfirmationModal));
            });

            ConfirmButton.onClick.AddListener(() =>
            {
                Find.State<MinigameRequestExitState>().ExitRequestState = RequestState.Confirmed;
            });

            ExitConfirmationModal.alpha = 0;
            ExitConfirmationModal.interactable = false;
            ExitConfirmationModal.blocksRaycasts = false;
        }
    }
}
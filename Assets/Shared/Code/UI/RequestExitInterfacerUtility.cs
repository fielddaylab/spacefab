using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public static class RequestExitInterfacerUtility
    {
        public static IEnumerator ShowExitConfirmationModal(CanvasGroup confirmationModal)
        {
            confirmationModal.alpha = 1;
            confirmationModal.blocksRaycasts = true;
            confirmationModal.interactable = true;
            yield break;
        }

        public static IEnumerator HideExitConfirmationModal(CanvasGroup confirmationModal)
        {
            confirmationModal.alpha = 0;
            confirmationModal.blocksRaycasts = false;
            confirmationModal.interactable = false;
            yield break;
        }
    }
}
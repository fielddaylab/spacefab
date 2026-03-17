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
            CompletionCanvasGroup.alpha = 0;
            SelectionCanvasGroup.alpha = 0;
            CompletedContractUI.gameObject.SetActive(false);
        }
    }

    public static class ContractLayoutUtility
    {
        public static IEnumerator EnterPreviousRoutine(ContractCompletionState completionState, ContractLayoutState layoutState)
        {
            layoutState.CompletedContractZone.anchoredPosition = layoutState.CompletedContractStartPos;
            layoutState.CompletionCanvasGroup.alpha = 0;
            layoutState.CompletedContractUI.gameObject.SetActive(true);

            yield return 0.5f;

            yield return Routine.Combine(
                layoutState.CompletionCanvasGroup.FadeTo(1, 0.5f),
                layoutState.CompletedContractZone.MoveTo(0, 1, Axis.Y, Space.Self).Ease(Curve.CubeIn)
                );

            yield return 0.5f;
        }

        public static IEnumerator EvaluatePreviousRoutine(ContractCompletionState completionState, ContractLayoutState layoutState)
        {
            yield return 0.5f;
        }

        public static IEnumerator HidePreviousRoutine(ContractCompletionState completionState, ContractLayoutState layoutState)
        {

            yield return 0.5f;

            yield return Routine.Combine(
                layoutState.CompletionCanvasGroup.FadeTo(0, 0.5f),
                layoutState.CompletedContractZone.MoveTo(layoutState.CompletedContractStartPos, 1, Axis.Y, Space.Self).Ease(Curve.CubeIn)
                );

            layoutState.CompletedContractUI.gameObject.SetActive(false);

            yield return 0.5f;

        }
    }
}
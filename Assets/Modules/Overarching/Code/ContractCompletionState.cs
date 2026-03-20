using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum ContractCompletionPhase
    {
        Waiting,
        Loading,
        EnterPreviousContract,
        EvaluatePreviousContract,
        HidePreviousContract,
        Completed
    }

    public class ContractCompletionState : SharedStateComponent
    {
        public ContractCompletionPhase Phase;
    }

    public static class ContractCompletionUtility
    {
        public static IEnumerator EnterPreviousRoutine(ContractLayoutState layoutState)
        {
            layoutState.FaderGroup.alpha = 1;
            layoutState.FaderGroup.blocksRaycasts = true;
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

        public static IEnumerator EvaluatePreviousRoutine(ContractLayoutState layoutState)
        {
            yield return 0.5f;
        }

        public static IEnumerator HidePreviousRoutine(ContractLayoutState layoutState)
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
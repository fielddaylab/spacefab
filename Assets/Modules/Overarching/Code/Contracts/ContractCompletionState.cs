using BeauRoutine;
using BeauUtil.Debugger;
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
        BeginLoadFromPrevChapter,
        LoadFromPrevChapter,
        EnterPreviousContract,
        EvaluatePreviousContract,
        HidePreviousContract,
        UnloadFromPrevChapter,
        Completed
    }

    public class ContractCompletionState : SharedStateComponent
    {
        public ContractCompletionPhase Phase;
    }

    public static class ContractCompletionUtility
    {
        public static IEnumerator LoadFromPrevChapterRoutine(ContractCompletionState completionState, ChapterState chapterState, AvailableContractsLookup lookup)
        {
            if (chapterState.CurrChapterIndex <= 0) {
                Log.Error("ContractCompletionState] Attempted to load a previous chapter when none exists!");
                yield break;
            }

            // Load previously available contracts
            yield return ContractsLookupUtility.LoadAvailableContractsAtChapter(lookup, chapterState, chapterState.CurrChapterIndex - 1);

            yield break;
        }

        public static IEnumerator UnloadFromPrevChapterRoutine(ContractCompletionState completionState, ChapterState chapterState, AvailableContractsLookup lookup)
        {
            if (chapterState.CurrChapterIndex <= 0)
            {
                Log.Error("ContractCompletionState] Attempted to unload a previous chapter when none exists!");
                yield break;
            }

            // Load previously available contracts
            yield return ContractsLookupUtility.UnloadAvailableContractsAtChapter(lookup, chapterState, chapterState.CurrChapterIndex - 1);

            yield break;
        }

        public static void PopulateContractUI(ContractCompletionState completionState, ContractLayoutState layoutState, ChapterState chapterState, AvailableContractsLookup lookup)
        {
            ContractDef contractDef = chapterState.CurrAvailableContractsBundle.AvailableContracts[chapterState.LastSelectedContractIndex];
            ContractUtility.LoadContractData(layoutState.CompletedContractUI, contractDef);
        }

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

        public static IEnumerator EvaluatePreviousRoutine(ContractLayoutState layoutState, PlayerProgressState progressState)
        {
            yield return 0.5f;

            progressState.CompletedContractIds.Add(progressState.CurrContractId);
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
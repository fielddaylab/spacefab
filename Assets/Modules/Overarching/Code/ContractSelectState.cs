using BeauRoutine;
using BeauUtil;
using FieldDay;
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
        public int SelectedContractIndex;
        public bool SelectionConfirmed;
    }

    public static class ContractSelectUtility
    {
        public static IEnumerator PresentAvailableRoutine(ContractSelectState selectState, ContractLayoutState layoutState)
        {
            yield return 0.5f;

            selectState.SelectionConfirmed = false;

            layoutState.SelectionCanvasGroup.alpha = 0;
            // filter active based on number of available contracts

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f)
                );

            yield return 0.5f;
        }

        public static IEnumerator ConfirmContractRoutine(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState)
        {
            chapterState.CurrSelectedContractAssetPack = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectState.SelectedContractIndex].ContractAssets();
            Game.Assets.LoadPackage(chapterState.CurrSelectedContractAssetPack);

            yield return 0.5f;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(0, 1f)
            );

            yield return 0.5f;

            layoutState.FaderGroup.alpha = 0;
        }
    }
}
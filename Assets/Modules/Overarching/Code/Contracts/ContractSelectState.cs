using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scripting;
using FieldDay.SharedState;
using SpaceFab.Design;
using SpaceFab.Save;
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
        Completed
    }

    public class ContractSelectState : SharedStateComponent
    {
        public ContractSelectPhase Phase;
        public int SelectedContractIndex;
        public bool SelectedContractIndexChanged;
        public bool SelectionConfirmed;
    }

    public static class ContractSelectUtility
    {
        public static IEnumerator PresentAvailableRoutine(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState, PlayerProgressState playerProgress)
        {
            layoutState.FaderGroup.alpha = 1;
            layoutState.FaderGroup.blocksRaycasts = true;

            yield return 0.5f;

            selectState.SelectedContractIndex = 0;
            selectState.SelectionConfirmed = false;
            selectState.SelectedContractIndexChanged = true;

            layoutState.ConfirmContractButton.gameObject.SetActive(true);
            layoutState.ChangeContractButton.gameObject.SetActive(false);

            layoutState.SelectionCanvasGroup.alpha = 0;

            ContractUtility.LoadContractData(layoutState.SelectionContractUI, null);
            layoutState.SelectionContractUI.gameObject.SetActive(true);
            layoutState.SelectionCanvasGroup.blocksRaycasts = true;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f)
                );

            yield return 0.5f;

            ScriptUtility.Trigger("OnContractSelectOpen");
        }

        public static void LoadAvailableContractIntoOptionButton(ContractOptionButton optionBtn, ContractDef contract) 
        {
            optionBtn.ContractTitle.SetText(contract.Title());
        }
    }
}
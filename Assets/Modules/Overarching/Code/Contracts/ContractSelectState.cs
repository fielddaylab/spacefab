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
using TMPro;
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
        public TMP_Text ContractTitleText;
    }

    public static class ContractSelectUtility
    {
        public static IEnumerator PresentAvailableRoutine(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState, PlayerProgressState playerProgress)
        {
            layoutState.FaderGroup.alpha = 1;
            layoutState.FaderGroup.blocksRaycasts = true;
            layoutState.ConfirmContractButton.gameObject.SetActive(false);

            yield return 0.5f;

            // Reopen on the contract the player last accepted so the selection index and the
            // panel agree — the change-contract flow compares the two to detect an actual swap.
            int index = chapterState.LastSelectedContractIndex < 0 ? 0 : chapterState.LastSelectedContractIndex;

            selectState.SelectedContractIndex = index;
            selectState.SelectionConfirmed = false;
            selectState.SelectedContractIndexChanged = true;

            layoutState.ChangeContractButton.gameObject.SetActive(false);

            layoutState.SelectionCanvasGroup.alpha = 0;

            ContractDef contract = ContractUtility.GetDefinition(chapterState.ChapterDefinition.AvailableContracts[index]);
            ContractUtility.LoadContractData(layoutState.SelectionContractUI, contract);
            
            layoutState.SelectionContractUI.gameObject.SetActive(true);
            layoutState.SelectionCanvasGroup.blocksRaycasts = true;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f)
                );

            yield return 0.5f;

            ScriptUtility.Trigger("OnContractSelectOpen");
        }
    }
}
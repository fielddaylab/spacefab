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

            selectState.SelectedContractIndex = -1;
            selectState.SelectionConfirmed = false;
            layoutState.ConfirmContractButton.interactable = false;

            layoutState.ConfirmContractButton.gameObject.SetActive(true);
            layoutState.ChangeContractButton.gameObject.SetActive(false);

            layoutState.ContractOptionsZone.anchoredPosition = layoutState.ContractOptionsStartPos;
            layoutState.SelectionCanvasGroup.alpha = 0;

            // filter active based on number of available contracts
            for (int i = 0; i < layoutState.OptionButtons.Length; i++)
            {
                ;
                if (i >= chapterState.CurrAvailableContractsBundle.AvailableContracts.Length)
                {
                    layoutState.OptionButtons[i].gameObject.SetActive(false);
                }
                else
                {
                    // filter out completed contracts
                    if (PlayerProgressUtility.HasCompletedContract(playerProgress, chapterState.CurrAvailableContractsBundle.AvailableContracts[i].AssetId))
                    {
                        layoutState.OptionButtons[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        layoutState.OptionButtons[i].gameObject.SetActive(true);
                        LoadAvailableContractIntoOptionButton(layoutState.OptionButtons[i], chapterState.CurrAvailableContractsBundle.AvailableContracts[i]);
                    }
                }
            }

            ContractUtility.LoadContractData(layoutState.SelectionContractUI, null);
            layoutState.SelectionContractUI.gameObject.SetActive(true);
            layoutState.SelectionCanvasGroup.blocksRaycasts = true;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f),
                layoutState.ContractOptionsZone.MoveTo(layoutState.ContractOptionsEndPos, 1, Axis.X, Space.Self).Ease(Curve.CubeIn)
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
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
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
        public static IEnumerator PresentAvailableRoutine(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState)
        {
            yield return 0.5f;

            selectState.SelectedContractIndex = -1;
            selectState.SelectionConfirmed = false;
            layoutState.ConfirmContractButton.interactable = false;
            
            layoutState.ContractOptionsZone.anchoredPosition = layoutState.ContractOptionsStartPos;
            layoutState.SelectionCanvasGroup.alpha = 0;

            // filter active based on number of available contracts
            for (int i = 0; i < layoutState.OptionButtons.Length; i++)
            {
                if (i >= chapterState.CurrAvailableContractsBundle.AvailableContracts.Length)
                {
                    layoutState.OptionButtons[i].gameObject.SetActive(false);
                }
                else
                {
                    layoutState.OptionButtons[i].gameObject.SetActive(true);
                }
            }

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f),
                layoutState.ContractOptionsZone.MoveTo(layoutState.ContractOptionsEndPos, 1, Axis.X, Space.Self).Ease(Curve.CubeIn)
                );

            yield return 0.5f;
        }

        public static IEnumerator ConfirmContractRoutine(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState)
        {
            chapterState.CurrSelectedContractAssetPack = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectState.SelectedContractIndex].ContractAssets();
            Game.Assets.LoadPackage(chapterState.CurrSelectedContractAssetPack);

            // Unpack further
            StringHash32 assetsWrapperId = chapterState.CurrAvailableContractsBundle.AvailableContracts[selectState.SelectedContractIndex].ContractAssetsWrapperId;
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(assetsWrapperId);
            // design level starts as initial config by default
            var minigameSaveState = Find.State<MinigameSaveStates>();
            minigameSaveState.Design.GridStack = new GridStack();
            GridStackUtility.LoadConfig(ref minigameSaveState.Design.GridStack, contractAssets.DesignLevelData.GetGridConfig());

            yield return 0.5f;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(0, 1f)
            );

            yield return 0.5f;

            layoutState.FaderGroup.alpha = 0;
            layoutState.FaderGroup.blocksRaycasts = false;
        }
    }
}
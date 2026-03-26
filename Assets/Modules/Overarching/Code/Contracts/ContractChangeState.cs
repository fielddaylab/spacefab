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
    public enum ContractChangePhase
    {
        Waiting,
        Starting,
        Viewing,
        ContractSelectSystem,
        DoubleConfirmContract,
        DoubleCancelContract,
        ContractConfirmSystem,
        ContractLoadSystem,
        Docking,
        Completed
    }

    public class ContractChangeState : SharedStateComponent, IRegistrationCallbacks
    {
        public ContractChangePhase Phase;
        [HideInInspector] public int StashedSelectedContractIndex;
        [HideInInspector] public bool ChangeDoubleConfirmed;

        public Routine TransitionRoutine;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            StashedSelectedContractIndex = -1;
        }
    }

    public static class ContractChangeUtility
    {
        public static IEnumerator ViewCurrentRoutine(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState)
        {

            layoutState.ViewCurrContractButton.gameObject.SetActive(false);
            layoutState.HideCurrContractButton.gameObject.SetActive(true);

            layoutState.FaderGroup.alpha = 1;
            layoutState.FaderGroup.blocksRaycasts = true;

            layoutState.ContractOptionsZone.anchoredPosition = layoutState.ContractOptionsStartPos;
            layoutState.ConfirmContractButton.gameObject.SetActive(false);
            layoutState.ChangeContractButton.gameObject.SetActive(true);

            yield return 0.5f;

            layoutState.SelectionCanvasGroup.alpha = 0;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f)
                );

            yield return 0.5f;
        }

        public static IEnumerator DockContractRoutine(ContractChangeState changeState, ContractLayoutState layoutState)
        {
            yield return Routine.Combine(
                layoutState.DoubleConfirmCanvasGroup.FadeTo(0, 1f),
                layoutState.SelectionCanvasGroup.FadeTo(0, 1f),
                layoutState.FaderGroup.FadeTo(0, 1f)
                );

            layoutState.FaderGroup.alpha = 0;
            layoutState.FaderGroup.blocksRaycasts = false;

            GameLoop.SuspendUpdates(UpdateMasks.ContractSystemsMask);
            layoutState.ViewCurrContractButton.gameObject.SetActive(true);
            layoutState.HideCurrContractButton.gameObject.SetActive(false);

            changeState.Phase = ContractChangePhase.Completed;
            /*
            chapterState.LastSelectedContractIndex = selectState.SelectedContractIndex;
            StringHash32 contractId = chapterState.CurrAvailableContractsBundle.AvailableContracts[chapterState.LastSelectedContractIndex].AssetId;

            yield return ContractsLookupUtility.LoadContract(lookup, contractId);
            ContractsLookupUtility.Lookup(lookup, contractId, out SceneReference contractAssetsScene, out StringHash32 assetsWrapperId);

            // Extract assets into game states
            var contractAssets = Find.NamedAsset<ContractAssetsWrapper>(assetsWrapperId);
            // design level starts as initial config by default
            var minigameSaveState = Find.State<MinigameSaveStates>();
            minigameSaveState.Design.GridStack = new GridStack();
            GridStackUtility.LoadConfig(ref minigameSaveState.Design.GridStack, contractAssets.DesignLevelData.GetGridConfig());

            SaveUtility.Save(SaveSlot.Main);

            yield return 0.5f;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(0, 1f)
            );

            yield return 0.5f;

            layoutState.FaderGroup.alpha = 0;
            layoutState.FaderGroup.blocksRaycasts = false;
            */
            yield break;
        }

        public static IEnumerator CancelChangeRoutine(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState)
        {
            changeState.Phase = ContractChangePhase.Starting;
            selectState.SelectedContractIndex = changeState.StashedSelectedContractIndex;
            changeState.StashedSelectedContractIndex = -1;
            layoutState.DoubleConfirmCanvasGroup.alpha = 0;

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
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.SharedState;
using FieldDay.UI;
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
            TransitionRoutine.Stop();
        }

        public void OnRegister()
        {
            StashedSelectedContractIndex = -1;
        }
    }

    public static class ContractChangeUtility
    {
        public static IEnumerator ViewCurrentRoutine(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState)
        {

            layoutState.ViewCurrContractButton.gameObject.SetActive(false);
            layoutState.HideCurrContractButton.gameObject.SetActive(true);

            layoutState.FaderGroup.alpha = 1;
            layoutState.FaderGroup.blocksRaycasts = true;

            layoutState.ContractOptionsZone.anchoredPosition = layoutState.ContractOptionsStartPos;
            layoutState.ConfirmContractButton.gameObject.SetActive(false);
            layoutState.ChangeContractButton.gameObject.SetActive(true);

            ContractUtility.LoadContractData(layoutState.SelectionContractUI, chapterState.CurrAvailableContractsBundle.AvailableContracts[chapterState.LastSelectedContractIndex]);
            layoutState.SelectionContractUI.gameObject.SetActive(true);

            yield return 0.5f;

            layoutState.SelectionCanvasGroup.alpha = 0;
            layoutState.SelectionCanvasGroup.blocksRaycasts = true;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(1, 1f)
                );

            yield return 0.5f;
        }

        public static IEnumerator DockContractRoutine(ContractChangeState changeState, ContractLayoutState layoutState, SharedUIState sharedUIState)
        {
            layoutState.SelectionContractUI.gameObject.SetActive(false);
            layoutState.SelectionCanvasGroup.blocksRaycasts = false;

            yield return Routine.Combine(
                layoutState.SelectionCanvasGroup.FadeTo(0, 1f),
                layoutState.FaderGroup.FadeTo(0, 1f)
                );

            layoutState.FaderGroup.alpha = 0;
            layoutState.FaderGroup.blocksRaycasts = false;


            GameLoop.SuspendUpdates(UpdateMasks.ContractSystemsMask);
            layoutState.ViewCurrContractButton.gameObject.SetActive(true);
            layoutState.HideCurrContractButton.gameObject.SetActive(false);

            changeState.Phase = ContractChangePhase.Completed;
        }

        public static IEnumerator CancelChangeRoutine(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState)
        {
            changeState.Phase = ContractChangePhase.Starting;
            selectState.SelectedContractIndex = changeState.StashedSelectedContractIndex;
            changeState.StashedSelectedContractIndex = -1;
            layoutState.DoubleConfirmCanvasGroup.alpha = 0;
            layoutState.DoubleConfirmCanvasGroup.blocksRaycasts = false;

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
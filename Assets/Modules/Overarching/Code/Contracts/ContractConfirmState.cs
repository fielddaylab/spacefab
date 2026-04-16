using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum ContractConfirmPhase
    {
        Waiting,
        Confirming,
        Completed
    }

    public class ContractConfirmState : SharedStateComponent, IRegistrationCallbacks
    {
        public ContractConfirmPhase Phase;

        public Routine ConfirmRoutine;

        public void OnDeregister()
        {
            ConfirmRoutine.Stop();
        }

        public void OnRegister()
        {
        }
    }

    public static class ContractConfirmUtility
    {
        public static IEnumerator ConfirmContractRoutine(ContractConfirmState confirmState, ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState, ContractAssetsLookup lookup, SharedUIState sharedUIState)
        {
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

            layoutState.SelectionCanvasGroup.blocksRaycasts = false;

            yield return 0.5f;

            layoutState.FaderGroup.alpha = 0;
            layoutState.FaderGroup.blocksRaycasts = false;

            confirmState.Phase = ContractConfirmPhase.Completed;
        }
    }
}

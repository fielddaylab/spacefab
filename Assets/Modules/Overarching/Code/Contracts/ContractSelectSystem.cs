using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Runs the contract-selection UI flow: presents available contracts, tracks the selected
    /// index, keeps the confirm button in sync, and advances to Completed once the selection
    /// is confirmed. Runs on Update at order -10 under ContractSystemsMask.
    /// </summary>
    public class ContractSelectSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, -10, UpdateMasks.ContractSystemsMask),
                new SysPermissions()
                    .ReadWriteShared<ContractSelectState>()
                    .ReadWriteShared<ContractLayoutState>()
                    .ReadShared<ChapterState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        // Dispatches to the handler for the current select phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ContractSelectState selectState,
                out ContractLayoutState layoutState,
                out ChapterState chapterState,
                out PlayerProgressState progressState
                );

            switch (selectState.Phase) {
                case ContractSelectPhase.Loading:
                    ProcessLoading(selectState, layoutState, chapterState, progressState);
                    break;
                case ContractSelectPhase.PresentAvailableContracts:
                    ProcessPresentAvailableContracts(selectState, layoutState);
                    break;
                case ContractSelectPhase.SelectContract:
                    ProcessSelectContract(selectState, layoutState, chapterState);
                    break;
                default:
                    break;
            }
        }

        // Kicks off the present-available-contracts routine and advances the phase.
        static private void ProcessLoading(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState, PlayerProgressState progressState) {
            layoutState.SelectionRoutine.Replace(ContractSelectUtility.PresentAvailableRoutine(selectState, layoutState, chapterState, progressState));
            selectState.Phase = ContractSelectPhase.PresentAvailableContracts;
        }

        // Waits for the presentation routine to finish, then allows contract selection.
        static private void ProcessPresentAvailableContracts(ContractSelectState selectState, ContractLayoutState layoutState) {
            if (!layoutState.SelectionRoutine.Exists()) {
                selectState.Phase = ContractSelectPhase.SelectContract;
            }
        }

        // Reflects the player's ongoing selection in the UI, refreshes contract data on change, and completes on confirm.
        static private void ProcessSelectContract(ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState) {
            // Enable the Confirm button as soon as any valid contract is selected
            if (selectState.SelectedContractIndex != -1 && layoutState.ConfirmContractButton.interactable == false) {
                layoutState.ConfirmContractButton.interactable = true;
            }
            // When the selected contract changes, refresh its detail UI
            if (selectState.SelectedContractIndexChanged) {
                ContractUtility.LoadContractData(layoutState.SelectionContractUI, chapterState.CurrAvailableContractsBundle.AvailableContracts[selectState.SelectedContractIndex]);
                selectState.SelectedContractIndexChanged = false;
            }
            // Confirmed — advance the phase
            if (selectState.SelectionConfirmed) {
                selectState.Phase = ContractSelectPhase.Completed;
            }
        }
    }
}

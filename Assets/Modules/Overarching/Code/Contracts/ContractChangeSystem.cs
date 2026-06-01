using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Drives the change-contract flow once the player opts to switch the current contract:
    /// views the current contract, defers to ContractSelectSystem for a new pick, double-confirms
    /// the swap, defers to ContractConfirmSystem, then docks. Runs on Update at order -11 under
    /// ContractSystemsMask.
    /// </summary>
    public class ContractChangeSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, -11, UpdateMasks.ContractSystemsMask),
                new SysPermissions()
                    .ReadWriteShared<ContractChangeState>()
                    .ReadWriteShared<ContractSelectState>()
                    .ReadWriteShared<ContractLayoutState>()
                    .ReadShared<ChapterState>()
                    .ReadWriteShared<ContractConfirmState>()
                    .ReadShared<SharedUIState>()
            );
        }

        // Dispatches to the handler for the current change phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ContractChangeState changeState,
                out ContractSelectState selectState,
                out ContractLayoutState layoutState,
                out ChapterState chapterState
                );
            Find.State(
                out ContractConfirmState confirmState,
                out SharedUIState uiState
                );

            switch (changeState.Phase) {
                case ContractChangePhase.Starting:
                    ProcessStarting(changeState, selectState, layoutState, chapterState);
                    SpacefabGame.Events.Dispatch(GameEvents.OpenContractView);
                    break;
                /*case ContractChangePhase.Viewing:
                    break;*/
                case ContractChangePhase.ContractSelectSystem:
                    ProcessContractSelectSystem(changeState, selectState, layoutState, chapterState, confirmState);
                    break;
                case ContractChangePhase.DoubleConfirmContract:
                    ProcessDoubleConfirmContract(changeState, confirmState);
                    SpacefabGame.Events.Dispatch(GameEvents.ConfirmSelectContract, changeState.StashedSelectedContractIndex.ToString());
                    SpacefabGame.Events.Dispatch(GameEvents.AcceptContract, selectState.SelectedContractIndex.ToString());
                    Debug.Log("[ContractChangeSystem] dispatch new contract: " + selectState.SelectedContractIndex);
                    break;
                case ContractChangePhase.DoubleCancelContract:
                    ProcessDoubleCancelContract(changeState, selectState, layoutState);
                    Debug.Log("[ContractChangeSystem] dispatch cancel change");
                    SpacefabGame.Events.Dispatch(GameEvents.CancelSelectContract, changeState.StashedSelectedContractIndex.ToString());
                    break;
                case ContractChangePhase.ContractConfirmSystem:
                    ProcessContractConfirmSystem(changeState, layoutState, confirmState);
                    break;
                case ContractChangePhase.Docking:
                    ProcessDocking(changeState, layoutState, uiState);
                    break;
                default:
                    break;
            }
        }

        // Entry: queue a view-current routine and advance to Viewing.
        static private void ProcessStarting(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState) {
            Debug.Log("[ContractChangeSystem] Starting");
            selectState.Phase = ContractSelectPhase.Waiting;
            changeState.ChangeDoubleConfirmed = false;
            changeState.TransitionRoutine.Replace(ContractChangeUtility.ViewCurrentRoutine(changeState, selectState, layoutState, chapterState));
            changeState.Phase = ContractChangePhase.Viewing;
        }

        // Coordinates with ContractSelectSystem: hands off to it when Waiting, reacts when Completed.
        static private void ProcessContractSelectSystem(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState, ContractConfirmState confirmState) {
            if (selectState.Phase == ContractSelectPhase.Waiting) {
                changeState.StashedSelectedContractIndex = selectState.SelectedContractIndex;
                selectState.Phase = ContractSelectPhase.Loading;
                confirmState.Phase = ContractConfirmPhase.Waiting;
                layoutState.HideCurrContractButton.gameObject.SetActive(false);
                Debug.Log("[ContractChangeSystem] Deferring to ContractSelectSystem");
                SpacefabGame.Events.Dispatch(GameEvents.StartSelectContract, changeState.StashedSelectedContractIndex.ToString());
            }
            else if (selectState.Phase == ContractSelectPhase.Completed) {
                if (selectState.SelectedContractIndex == chapterState.LastSelectedContractIndex) {
                    // no change
                    changeState.Phase = ContractChangePhase.Docking;
                }
                else {
                    // Show the double-confirm overlay and wait for the player
                    layoutState.DoubleConfirmCanvasGroup.blocksRaycasts = true;
                    layoutState.DoubleConfirmCanvasGroup.alpha = 1;
                    Debug.Log("[ContractChangeSystem] Double Confirming Change");
                    changeState.Phase = ContractChangePhase.DoubleConfirmContract;
                }
            }
        }

        // Wait for the double-confirm flag, then hand off to the confirm subsystem.
        static private void ProcessDoubleConfirmContract(ContractChangeState changeState, ContractConfirmState confirmState) {
            if (changeState.ChangeDoubleConfirmed) {
                changeState.Phase = ContractChangePhase.ContractConfirmSystem;
                confirmState.Phase = ContractConfirmPhase.Waiting;
            }
        }

        // Player cancelled the swap — play the cancel routine once.
        static private void ProcessDoubleCancelContract(ContractChangeState changeState, ContractSelectState selectState, ContractLayoutState layoutState) {
            if (!changeState.TransitionRoutine.Exists()) {
                changeState.TransitionRoutine.Replace(ContractChangeUtility.CancelChangeRoutine(changeState, selectState, layoutState));
                Debug.Log("[ContractChangeSystem] Canceling Change");
            }
        }

        // Coordinates with ContractConfirmSystem: hands off on Waiting, advances to Docking on Completed.
        static private void ProcessContractConfirmSystem(ContractChangeState changeState, ContractLayoutState layoutState, ContractConfirmState confirmState) {
            if (confirmState.Phase == ContractConfirmPhase.Waiting) {
                Debug.Log("[ContractChangeSystem] Deferring to ContractConfirmSystem");
                layoutState.DoubleConfirmCanvasGroup.alpha = 0;
                layoutState.DoubleConfirmCanvasGroup.blocksRaycasts = false;
                confirmState.Phase = ContractConfirmPhase.Confirming;
            }
            else if (confirmState.Phase == ContractConfirmPhase.Completed) {
                SpacefabGame.Events.Dispatch(GameEvents.ConfirmSelectContract, changeState.StashedSelectedContractIndex.ToString());
                Debug.Log("[ContractChangeSystem] ContractConfirmSystem completed");
                changeState.Phase = ContractChangePhase.Docking;
            }
        }

        // Runs the dock routine to tuck the contract UI back into its docked state.
        static private void ProcessDocking(ContractChangeState changeState, ContractLayoutState layoutState, SharedUIState uiState) {
            Debug.Log("[ContractChangeSystem] Docking Contract");
            if (!changeState.TransitionRoutine.Exists()) {
                changeState.TransitionRoutine.Replace(ContractChangeUtility.DockContractRoutine(changeState, layoutState, uiState));
            }
        }
    }
}

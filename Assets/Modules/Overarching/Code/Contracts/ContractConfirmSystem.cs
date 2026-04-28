using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Drives the confirm-contract flow: once Phase is Confirming, starts the confirm routine
    /// which records the selected contract, loads its assets, and fades out the selection UI.
    /// Runs on Update at order -9 under ContractSystemsMask.
    /// </summary>
    public class ContractConfirmSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, -9, UpdateMasks.ContractSystemsMask),
                new SysPermissions()
                    .ReadWriteShared<ContractConfirmState>()
                    .ReadShared<ContractSelectState>()
                    .ReadWriteShared<ContractLayoutState>()
                    .ReadWriteShared<ChapterState>()
                    .ReadWriteShared<ContractAssetsLookup>()
                    .ReadShared<SharedUIState>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        // Delegates to phase-specific handlers.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ContractConfirmState confirmState,
                out ContractSelectState selectState,
                out ContractLayoutState layoutState,
                out ChapterState chapterState
                );
            Find.State(
                out ContractAssetsLookup assetsLookup,
                out SharedUIState uiState,
                out PlayerProgressState playerProgress
                );

            switch (confirmState.Phase) {
                case ContractConfirmPhase.Confirming:
                    ProcessConfirming(confirmState, selectState, layoutState, chapterState, assetsLookup, uiState, playerProgress);
                    break;
                default:
                    break;
            }
        }

        // Starts the confirmation coroutine if one isn't already running.
        static private void ProcessConfirming(ContractConfirmState confirmState, ContractSelectState selectState, ContractLayoutState layoutState, ChapterState chapterState, ContractAssetsLookup assetsLookup, SharedUIState uiState, PlayerProgressState playerProgress) {
            if (!confirmState.ConfirmRoutine.Exists()) {
                confirmState.ConfirmRoutine.Replace(ContractConfirmUtility.ConfirmContractRoutine(confirmState, selectState, layoutState, chapterState, assetsLookup, uiState, playerProgress));
            }
        }
    }
}

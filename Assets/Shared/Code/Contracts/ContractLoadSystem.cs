using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Loads the currently-selected contract's assets and reveals its UI entry point.
    /// Runs in Update under ContractSystemsMask and steps ContractLoadState.Phase from
    /// BeginLoad through Loading to Completed.
    /// </summary>
    public class ContractLoadSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ContractSystemsMask),
                new SysPermissions()
                    .ReadWriteShared<ContractLoadState>()
                    .ReadWriteShared<ContractAssetsLookup>()
                    .ReadShared<ChapterState>()
                    .ReadShared<ContractLayoutState>()
            );
        }

        // Dispatches to the handler for the current contract-load phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ContractLoadState loadState,
                out ContractAssetsLookup assetsLookup,
                out ChapterState chapterState,
                out ContractLayoutState layoutState
                );

            switch (loadState.Phase) {
                case ContractLoadPhase.BeginLoad:
                    ProcessBeginLoad(loadState, assetsLookup, chapterState);
                    break;
                case ContractLoadPhase.Loading:
                    ProcessLoading(loadState, layoutState);
                    break;
                default:
                    break;
            }
        }

        // Kicks off the load routine for the currently-selected contract and advances to the Loading phase.
        static private void ProcessBeginLoad(ContractLoadState loadState, ContractAssetsLookup assetsLookup, ChapterState chapterState) {
            StringHash32 contractId = chapterState.CurrAvailableContractsBundle.AvailableContracts[chapterState.LastSelectedContractIndex].AssetId;
            loadState.LoadRoutine.Replace(ContractsLookupUtility.LoadContract(assetsLookup, contractId));
            loadState.Phase = ContractLoadPhase.Loading;
        }

        // Waits for the load routine to finish, then reveals the view-contract button and marks the load complete.
        static private void ProcessLoading(ContractLoadState loadState, ContractLayoutState layoutState) {
            if (!loadState.LoadRoutine.Exists()) {
                layoutState.ViewCurrContractButton.gameObject.SetActive(true);
                loadState.Phase = ContractLoadPhase.Completed;
            }
        }
    }
}

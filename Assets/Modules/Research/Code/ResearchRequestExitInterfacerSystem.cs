using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Handles the Research minigame's exit-request flow: currently short-circuits a Requested
    /// state straight to Confirmed, and hides the exit confirmation modal once confirmed.
    /// Runs on Update phase at order 0, no category mask.
    /// </summary>
    public class ResearchRequestExitInterfacerSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<ResearchRequestExitInterfacerState>()
                    .ReadWriteShared<MinigameRequestExitState>()
            );
        }

        // Reacts to the current exit-request state: confirms requests and hides the modal on confirmation.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchRequestExitInterfacerState researchInterfacerState,
                out MinigameRequestExitState requestExitState
                );

            if (requestExitState.ExitRequestState == RequestState.Requested) {
                /*
                researchInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.ShowExitConfirmationModal(researchInterfacerState.ExitConfirmationModal));

                requestExitState.ExitRequestState = RequestState.Pending;
                */
                requestExitState.ExitRequestState = RequestState.Confirmed;
            }
            else if (requestExitState.ExitRequestState == RequestState.Confirmed) {
                researchInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.HideExitConfirmationModal(researchInterfacerState.ExitConfirmationModal));
            }
        }
    }
}

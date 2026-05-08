using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Handles the Fabrication minigame's exit-request flow: shows the exit confirmation modal
    /// when a request comes in, and hides it once the request is confirmed.
    /// Runs on Update phase at order 0, no category mask.
    /// </summary>
    public class FabricationRequestExitInterfacerSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<FabricationRequestExitInterfacerState>()
                    .ReadWriteShared<MinigameRequestExitState>()
            );
        }

        // Reacts to the current exit-request state: shows the modal on request, hides it on confirm.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out FabricationRequestExitInterfacerState fabricationInterfacerState,
                out MinigameRequestExitState requestExitState
                );

            if (requestExitState.ExitRequestState == RequestState.Requested) {
                fabricationInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.ShowExitConfirmationModal(fabricationInterfacerState.ExitConfirmationModal));

                requestExitState.ExitRequestState = RequestState.Pending;
            }
            else if (requestExitState.ExitRequestState == RequestState.Confirmed) {
                fabricationInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.HideExitConfirmationModal(fabricationInterfacerState.ExitConfirmationModal));
            }
        }
    }
}

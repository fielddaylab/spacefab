using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Handles the Design minigame's exit-request flow: shows the exit confirmation modal
    /// when a request comes in, and hides it once the request is confirmed.
    /// Runs on Update at order 0, no category mask.
    /// </summary>
    public class DesignRequestExitInterfacerSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<DesignRequestExitInterfacerState>()
                    .ReadWriteShared<MinigameRequestExitState>()
            );
        }

        // Reacts to the current exit-request state: shows the modal on request, hides it on confirm.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out DesignRequestExitInterfacerState designInterfacerState,
                out MinigameRequestExitState requestExitState
                );

            if (requestExitState.ExitRequestState == RequestState.Requested) {
                designInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.ShowExitConfirmationModal(designInterfacerState.ExitConfirmationModal));

                requestExitState.ExitRequestState = RequestState.Pending;
            }
            else if (requestExitState.ExitRequestState == RequestState.Confirmed) {
                designInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.HideExitConfirmationModal(designInterfacerState.ExitConfirmationModal));
            }
        }
    }
}

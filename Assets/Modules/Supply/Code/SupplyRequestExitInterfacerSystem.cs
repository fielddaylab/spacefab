using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Handles the Supply minigame's exit-request flow: currently short-circuits a Requested
    /// state straight to Confirmed, and hides the exit confirmation modal once confirmed.
    /// Runs on Update phase at order 0, no category mask.
    /// </summary>
    public class SupplyRequestExitInterfacerSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<SupplyRequestExitInterfacerState>()
                    .ReadWriteShared<MinigameRequestExitState>()
            );
        }

        // Reacts to the current exit-request state: confirms requests and hides the modal on confirmation.
        static private void ProcessWork(float deltaTime) {
            Find.State(out SupplyRequestExitInterfacerState supplyExitInterfacerState,
                out MinigameRequestExitState minigameExitState);

            if (minigameExitState.ExitRequestState == RequestState.Requested) {
                supplyExitInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.ShowExitConfirmationModal(supplyExitInterfacerState.ExitConfirmationModal));

                minigameExitState.ExitRequestState = RequestState.Pending;
            }
            else if (minigameExitState.ExitRequestState == RequestState.Confirmed) {
                supplyExitInterfacerState.ModalRoutine.Replace(RequestExitInterfacerUtility.HideExitConfirmationModal(supplyExitInterfacerState.ExitConfirmationModal));
            }
        }
    }
}

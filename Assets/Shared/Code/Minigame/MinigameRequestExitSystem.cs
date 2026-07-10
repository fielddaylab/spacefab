using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Listens for a confirmed exit-minigame request and hands control to the exit pipeline
    /// by flipping MinigameLoadExitState.Phase to Exiting and switching the active update mask
    /// to MinigameTransitionMask. Runs on any Update phase at order 10.
    /// </summary>
    public class MinigameRequestExitSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.Update, 10),
                new SysPermissions()
                    .ReadWriteShared<MinigameRequestExitState>()
            );
        }

        // Once the request is Confirmed, begin the exit flow and swap update masks.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out MinigameRequestExitState requestExitState
                );

            switch (requestExitState.ExitRequestState) {
                case RequestState.Requested:
                    break;
                case RequestState.Pending:
                    break;
                case RequestState.Confirmed:
                    // begin exit system
                    MinigameUtility.Exit();
                    requestExitState.ExitRequestState = RequestState.None;
                    break;
                case RequestState.None:
                default:
                    break;
            }
        }
    }
}

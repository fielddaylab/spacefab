using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Robot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Facilitates transitions between Modes. Sets up and shuts down relevant systems.
    /// Runs on Update phase at order -5 (before the setup systems this transitions into). Currently a stub.
    /// </summary>
    public class ModeTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, -5, UpdateMasks.PreAttemptMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<ModeState>()
            );
        }

        // TODO: implement mode transition logic.
        static private void ProcessWork(float deltaTime) {
            Find.State(out ModeState modeState);

            switch (modeState.CurrMode)
            {
                case LevelMode.PreAttempt:
                    ProcessPreAttempt(modeState);
                    break;
                case LevelMode.AttemptLeadIn:
                    ProcessAttemptLeadIn(modeState);
                    break;
                case LevelMode.Attempt:
                    ProcessAttempt(modeState);
                    break;
                case LevelMode.PostAttempt:
                    ProcessPostAttempt(modeState);
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        static private void ProcessPreAttempt(ModeState modeState)
        {
            // poll for move into Attempt
            if (Input.GetKeyDown(FabricationConsts.Activate))
            {
                Log.Msg("[ModeTransitionSystem] received input for new wafer -- entering Attempt Mode");

                // Move into AttemptLeadIn Mode -- will generate wafer, countdown to start
                ModeUtility.SetNewMode(modeState, LevelMode.AttemptLeadIn);
                GameLoop.SuspendUpdates(UpdateMasks.PreAttemptMask);
                GameLoop.ResumeUpdates(UpdateMasks.AttemptLeadInMask);
            }
        }

        static private void ProcessAttemptLeadIn(ModeState modeState)
        {
            // poll for leadin countdown completed
                // start timer
        }

        static private void ProcessAttempt(ModeState modeState)
        {
            // poll for move into post-attempt
            // poll for reset/checkpoint triggers
        }

        static private void ProcessPostAttempt(ModeState modeState)
        {
            // poll for reset triggers
        }

        #endregion // Helpers
    }
}

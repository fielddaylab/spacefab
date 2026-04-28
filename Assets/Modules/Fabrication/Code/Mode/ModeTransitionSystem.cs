using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Robot;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpaceFab.Fabrication {
    /// <summary>
    /// Facilitates transitions between Modes. Sets up and shuts down relevant systems.
    /// Runs on PreUpdate phase at order -10 (before the setup systems this transitions into). Currently a stub.
    /// </summary>
    public class ModeTransitionSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, -10, UpdateMasks.PreAttemptMask | UpdateMasks.AttemptLeadInMask | UpdateMasks.AttemptMask | UpdateMasks.PostAttemptMask),
                new SysPermissions()
                    .ReadWriteShared<ModeState>()
                    .ReadShared<CountdownState>()
                    .ReadShared<SequenceState>()
                    .ReadShared<InterruptState>()
                    .ReadWriteShared<MinigameRequestExitState>()
            );
        }

        // implements mode transition logic.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ModeState modeState,
                out CountdownState countdownState,
                out SequenceState sequenceState,
                out InterruptState interruptState
                );
            Find.State(out MinigameRequestExitState exitState);

            switch (modeState.CurrMode)
            {
                case LevelMode.PreAttempt:
                    ProcessPreAttempt(modeState);
                    break;
                case LevelMode.AttemptLeadIn:
                    ProcessAttemptLeadIn(modeState, countdownState);
                    break;
                case LevelMode.Attempt:
                    ProcessAttempt(modeState, sequenceState, interruptState);
                    break;
                case LevelMode.PostAttempt:
                    ProcessPostAttempt(modeState, interruptState, exitState);
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        static private void ProcessPreAttempt(ModeState modeState)
        {
            // TODO: poll for move into Attempt
            if (Input.GetKeyDown(FabricationConsts.Activate))
            {
                Log.Msg("[ModeTransitionSystem] received input for new wafer -- entering Attempt Mode");
                TransitionToAttemptLeadIn(modeState);
            }
        }

        static private void ProcessAttemptLeadIn(ModeState modeState, CountdownState countdownState)
        {
            // poll for leadin countdown completed
            if (countdownState.CountdownCompletedThisFrame)
            {
                // start timer
                Log.Msg("[ModeTransitionSystem] countdown completed. Moving to Attempt Mode");

                ModeUtility.SetNewMode(modeState, LevelMode.Attempt);
                GameLoop.SuspendUpdates(UpdateMasks.AttemptLeadInMask);
                GameLoop.ResumeUpdates(UpdateMasks.AttemptMask);
            }
        }

        static private void ProcessAttempt(ModeState modeState, SequenceState sequenceState, InterruptState interruptState)
        {
            // TODO: poll for move into post-attempt
            if (sequenceState.Status == SequenceStatus.Completed)
            {
                Log.Msg("[ModeTransitionSystem] Attempt completed. Moving to PostAttempt Mode");
                ModeUtility.SetNewMode(modeState, LevelMode.PostAttempt);
                GameLoop.SuspendUpdates(UpdateMasks.AttemptMask);
                GameLoop.ResumeUpdates(UpdateMasks.PostAttemptMask);
            }

            // TODO: poll for reset triggers
            if (interruptState.ResetRequestedThisFrame)
            {
                Log.Msg("[ModeTransitionSystem] Attempt reset. Moving to AttemptLeadIn Mode");
                ResetUtility.ResetAttempt();
                TransitionToAttemptLeadIn(modeState);
            }
            
            // TODO: poll for checkpoint triggers
            if (interruptState.RestoreCheckpointRequestedThisFrame)
            {
                Log.Msg("[ModeTransitionSystem] Attempt reset to checkpoint. Staying in Attempt Mode");
            }
        }

        static private void ProcessPostAttempt(ModeState modeState, InterruptState interruptState, MinigameRequestExitState exitState)
        {
            // TODO: poll for reset triggers
            if (interruptState.ResetRequestedThisFrame)
            {
                Log.Msg("[ModeTransitionSystem] PostAttempt completed. Resetting to AttemptLeadIn Mode");
                ResetUtility.ResetAttempt();
                TransitionToAttemptLeadIn(modeState);
            }

            // TODO: poll for finalize triggers
            if (interruptState.FinalizeAttemptRequestedThisFrame)
            {
                Log.Msg("[ModeTransitionSystem] PostAttempt completed. Finalizing and exiting level");
                exitState.ExitRequestState = RequestState.Confirmed;
            }
        }

        static private void TransitionToAttemptLeadIn(ModeState modeState)
        {
            // Move into AttemptLeadIn Mode -- will generate wafer, countdown to start
            ModeUtility.SetNewMode(modeState, LevelMode.AttemptLeadIn);
            GameLoop.SuspendUpdates(UpdateMasks.PreAttemptMask);
            GameLoop.ResumeUpdates(UpdateMasks.AttemptLeadInMask);
        }

        #endregion // Helpers
    }
}

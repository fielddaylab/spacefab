using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching {
    /// <summary>
    /// Sequences the transition from the overarching scene into a minigame: hands off to the
    /// shutdown subsystem, then suspends all updates, resumes MinigameTransitionMask, loads
    /// the chosen minigame's scene, and dispatches OnMinigameLoad.
    /// Runs on LateUpdate at order 0 under ShutdownMask.
    /// </summary>
    public class OverarchingToMinigameSequenceSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.ShutdownMask),
                new SysPermissions()
                    .ReadWriteShared<OverarchingToMinigameSequenceState>()
                    .ReadShared<MinigameZonesState>()
            );
        }

        // Dispatches to the handler for the current transition phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out OverarchingToMinigameSequenceState toMinigameState,
                out MinigameZonesState zonesState
                );

            switch (toMinigameState.Phase) {
                case OverarchingToMinigamePhase.Starting:
                    ProcessStarting(toMinigameState);
                    break;
                case OverarchingToMinigamePhase.ShutdownSequenceSystem:
                    ProcessShutdownSequenceSystem(toMinigameState);
                    break;
                case OverarchingToMinigamePhase.TransitionToMinigame:
                    ProcessTransitionToMinigame(toMinigameState, zonesState);
                    break;
                default:
                    break;
            }
        }

        // Entry: ask the shutdown subsystem to start and move to the waiting phase.
        static private void ProcessStarting(OverarchingToMinigameSequenceState toMinigameState) {
            toMinigameState.Phase = OverarchingToMinigamePhase.ShutdownSequenceSystem;
        }

        // Coordinates with the shutdown subsystem: trigger it on Waiting, advance when Complete.
        static private void ProcessShutdownSequenceSystem(OverarchingToMinigameSequenceState toMinigameState) {
            toMinigameState.Phase = OverarchingToMinigamePhase.TransitionToMinigame;
        }

        // Swap update masks, load the target minigame scene, and announce the load.
        static private void ProcessTransitionToMinigame(OverarchingToMinigameSequenceState toMinigameState, MinigameZonesState zonesState) {
            GameLoop.SuspendUpdates(Bits.All32);
            GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
            Game.Scenes.LoadMainScene(zonesState.Zones[zonesState.CurrSelectedIndex].MinigameScene);
            Game.Events.Dispatch(GameEvents.OnMinigameLoad);
            toMinigameState.Phase = OverarchingToMinigamePhase.TransitionComplete;
        }
    }
}

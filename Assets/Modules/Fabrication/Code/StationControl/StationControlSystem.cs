using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Robot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.StationControl {
    /// <summary>
    /// Drives the station-control phase machine. Detects slot-arrival / slot-departure from MovementState's
    /// one-frame SlotChangedThisFrame flag, advances timer-based transitions (EnteringMicrogame, ExitingMicrogame,
    /// Stunned), and flips MicrogameMask when crossing into/out of InMicrogame. Runs on Update at order 5 under
    /// AttemptMask (after MovementSystem at order 0, before WorldInteractSystem at order 10).
    /// </summary>
    public class StationControlSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 5, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWriteShared<StationControlState>()
                    .ReadShared<MovementState>()
                    .ReadWriteShared<RobotState>()
                    .ReadShared<LayoutState>()
                    .ReadShared<RobotVisualsState>()
            );
        }

        // Dispatches to the handler for the current station-control phase.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out StationControlState stationState,
                out MovementState movementState,
                out RobotState robotState,
                out LayoutState layoutState
                );
            Find.State(out RobotVisualsState visualsState);

            switch (stationState.Phase) {
                case StationControlPhase.Traveling:
                    ProcessTraveling(stationState, movementState, layoutState);
                    break;
                case StationControlPhase.AtStation:
                    ProcessAtStation(stationState, movementState);
                    break;
                case StationControlPhase.EnteringMicrogame:
                    ProcessEnteringMicrogame(stationState, deltaTime);
                    break;
                case StationControlPhase.InMicrogame:
                    ProcessInMicrogame(stationState);
                    break;
                case StationControlPhase.ExitingMicrogame:
                    ProcessExitingMicrogame(stationState, deltaTime);
                    break;
                case StationControlPhase.Stunned:
                    ProcessStunned(stationState, robotState, visualsState, deltaTime);
                    break;
                default:
                    break;
            }
        }

        // Watches for slot arrival (SlotChangedThisFrame && !IsTraveling). On arrival, caches the slot's
        // assigned interfacer and transitions to AtStation. Dispatches FabStationArrived.
        static private void ProcessTraveling(StationControlState stationState, MovementState movementState, LayoutState layoutState) {
            // TODO:
        }

        // Watches for slot departure (SlotChangedThisFrame && IsTraveling). On departure, clears
        // ActiveInterfacer and transitions to Traveling. Dispatches FabStationLeft.
        // Activate handling lives in StationControlUtility.RequestActivate (called from WorldInteractSystem).
        static private void ProcessAtStation(StationControlState stationState, MovementState movementState) {
            // TODO:
        }

        // Accumulates PhaseTimer; at EnterMicrogameDuration, calls EnterComplete on the interfacer,
        // resumes MicrogameMask, dispatches FabMicrogameEntered, and transitions to InMicrogame.
        static private void ProcessEnteringMicrogame(StationControlState stationState, float deltaTime) {
            // TODO:
        }

        // Watches for MicrogameCompletedThisFrame (normal finish) or CancelRequestedThisFrame (player cancel).
        // Either path: suspends MicrogameMask, calls BeginExit with the correct completedNormally flag,
        // dispatches FabMicrogameCompleted or FabMicrogameCancelled, transitions to ExitingMicrogame,
        // resets PhaseTimer.
        static private void ProcessInMicrogame(StationControlState stationState) {
            // TODO:
        }

        // Accumulates PhaseTimer; at ExitMicrogameDuration, calls ExitComplete on the interfacer,
        // clears ActiveInterfacer, dispatches FabStationExit, and transitions to AtStation.
        static private void ProcessExitingMicrogame(StationControlState stationState, float deltaTime) {
            // TODO:
        }

        // Accumulates PhaseTimer; at StunDuration, calls RobotUtility.RemoveStun, dispatches FabStunEnd,
        // and transitions to PostStunPhase.
        static private void ProcessStunned(StationControlState stationState, RobotState robotState, RobotVisualsState visualsState, float deltaTime) {
            // TODO:
        }
    }
}

using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Robot;
using SpaceFab.Fabrication.StationControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement {
    /// <summary>
    /// Holds data relevant to player movement. Tracks the robot's current slot position (with a TRAVELING
    /// sentinel), whether movement is enabled globally, the active move routine, and a one-frame flag for
    /// slot-index changes consumed by the station-control state machine.
    /// </summary>
    public class MovementState : SharedStateComponent, IRegistrationCallbacks {
        // -1 sentinel value indicates traveling
        public static int TRAVELING = -1;

        // index of the slot the robot is currently at.
        public int CurrSlotPosition;

        [HideInInspector] public bool MoveEnabled;

        public Routine MoveRoutine;

        // One-frame flag: set by MovementSystem whenever CurrSlotPosition is written (either becoming
        // TRAVELING at move-start or becoming a target index at move-end). Cleared by MovementRefreshSystem
        // in LateUpdate. Consumed by StationControlSystem to detect arrival/departure edges.
        [HideInInspector] public bool SlotChangedThisFrame;

        public void OnDeregister() {
        }

        public void OnRegister() {
            MoveEnabled = true;
        }
    }

    public static class MovementUtility {
        // Original 2-arg overload; kept for callers that don't consult the station-control state machine.
        public static bool CanMove(MovementState moveState, RobotState robotState) {
            return !IsTraveling(moveState) && !robotState.IsStunned && moveState.MoveEnabled;
        }

        // 3-arg overload consulted by MovementSystem. Combines the existing CanMove checks with the
        // station-control machine's AllowsMovement gate.
        public static bool CanMove(MovementState moveState, RobotState robotState, StationControlState stationState) {
            return CanMove(moveState, robotState) && StationControlUtility.AllowsMovement(stationState);
        }

        public static bool IsTraveling(MovementState state) {
            return state.CurrSlotPosition == MovementState.TRAVELING;
        }
    }
}

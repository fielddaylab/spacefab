using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Robot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement
{
    /// <summary>
    /// Holds data relevant to player movement.
    /// Includes input mappings, whether player can move, etc.
    /// </summary>
    public class MovementState : SharedStateComponent, IRegistrationCallbacks
    {
        // -1 sentinel value indicates traveling
        public static int TRAVELING = -1;

        // index of the slot the robot is currently at.
        public int CurrSlotPosition;

        [HideInInspector] public bool MoveEnabled;

        public Routine MoveRoutine;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            MoveEnabled = true;
        }
    }

    public static class MovementUtility
    {
        public static bool CanMove(MovementState moveState, RobotState robotState)
        {
            return !IsTraveling(moveState) && !robotState.IsStunned && moveState.MoveEnabled;
        }

        public static bool IsTraveling(MovementState state)
        {
            return state.CurrSlotPosition == MovementState.TRAVELING;
        }
    }
}
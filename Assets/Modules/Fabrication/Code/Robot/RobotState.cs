using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Robot
{
    public enum RobotStatus
    {
        Idle,
        Holding,
        Stunned
    }
    
    /// <summary>
    /// Holds robot (player avatar) data.
    /// </summary>
    public class RobotState : SharedStateComponent
    {
        [HideInInspector] public bool IsStunned = false;
    }

    public static class RobotUtility
    {
        public static void ApplyStun(RobotState robotState, RobotVisualsState visualsState)
        {
            if (robotState.IsStunned)
            {
                // handle stacked stun if desired
                return;
            }

            robotState.IsStunned = true;
            RobotVisualsUtility.ApplyStunVisuals(visualsState);
        }

        // Clears the stun flag and removes its visuals. Called by StationControlSystem when the Stunned
        // phase elapses, or by any other system that needs to end a stun early.
        public static void RemoveStun(RobotState robotState, RobotVisualsState visualsState)
        {
            if (!robotState.IsStunned)
            {
                return;
            }

            robotState.IsStunned = false;
            RobotVisualsUtility.RemoveStunVisuals(visualsState);
        }
    }
}
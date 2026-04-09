using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Robot
{
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
    }
}
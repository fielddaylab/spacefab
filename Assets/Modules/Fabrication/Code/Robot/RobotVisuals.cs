using FieldDay.Components;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Robot
{
    public class RobotVisualsState : SharedStateComponent
    {
        public GameObject RobotIdle, RobotHold;//, RobotStun;
    }

    /// <summary>
    /// Should operate downstream of RobotUtility functions
    /// </summary>
    public static class RobotVisualsUtility
    {
        public static void UpdateVisuals(RobotVisualsState visuals, RobotStatus status)
        {
            visuals.RobotIdle.SetActive(false);
            visuals.RobotHold.SetActive(false);
            //visuals.RobotStun.SetActive(false);

            switch (status)
            {
                case RobotStatus.Idle:
                    visuals.RobotIdle.SetActive(true);
                    break;
                case RobotStatus.Holding:
                    visuals.RobotHold.SetActive(true);
                    break;
                case RobotStatus.Stunned:
                    //visuals.RobotStun.SetActive(true);
                    break;
            }
        }
        
        
        public static void ApplyStunVisuals(RobotVisualsState visuals)
        {
            // TODO
        }

        public static void RemoveStunVisuals(RobotVisualsState visuals)
        {
            // TODO
        }
    }
}

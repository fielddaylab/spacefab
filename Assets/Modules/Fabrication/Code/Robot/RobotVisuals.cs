using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Fabrication.Robot
{
    public class RobotVisualsState : SharedStateComponent, IRegistrationCallbacks
    {
        public GameObject RobotIdle, RobotHold;//, RobotStun; (awaiting stun graphic)
        public string InitialSortingLayerName, GameSortingLayerName;

        // this is awkward, but the only way I can think to get it working without a separate canvas
        public void OnRegister() {
            RobotIdle.GetComponent<SpriteRenderer>().sortingLayerName = InitialSortingLayerName;
            RobotHold.GetComponent<SpriteRenderer>().sortingLayerName = InitialSortingLayerName;
            //RobotStun.GetComponent<SpriteRenderer>().sortingLayerName = InitialSortingLayerName;
        }

        public void OnDeregister()
        {

        }
    }

    /// <summary>
    /// Should operate downstream of RobotUtility functions
    /// </summary>
    public static class RobotVisualsUtility
    {
        public static void UpdateLayer(RobotVisualsState visuals)
        {
            visuals.RobotIdle.GetComponent<SpriteRenderer>().sortingLayerName = visuals.GameSortingLayerName;
            visuals.RobotHold.GetComponent<SpriteRenderer>().sortingLayerName = visuals.GameSortingLayerName;
            //visuals.RobotStun.GetComponent<SpriteRenderer>().sortingLayerName = visuals.GameSortingLayerName;
        }
        
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

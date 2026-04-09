using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Robot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Movement
{
    /// <summary>
    /// Holds data for world (non-microgame) interactions and inputs.
    /// </summary>
    public class WorldInteractState : SharedStateComponent, IRegistrationCallbacks
    {

        [HideInInspector] public bool WorldInteractEnabled;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            WorldInteractEnabled = true;
        }
    }

    public static class WorldInteractUtility
    {

    }
}
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Facilitates transitioning between Design minigame modes.
    /// Modes include Tool Mode and Simulate Mode.
    /// </summary>
    public class ModeTransitionSystem : SharedStateSystemBehaviour<ModeTransitionState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe SystemFunctionShim GetDelegate() {
            return new SystemFunctionShim(&ProcessWork);
        }
    }
}
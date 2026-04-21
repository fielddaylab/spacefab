using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Facilitates transitions between Modes.
    /// Sets up and shuts down relevant systems.
    /// </summary>
    public class ModeTransitionSystem : SharedStateSystemBehaviour<ModeState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
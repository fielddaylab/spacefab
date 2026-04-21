using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages transitioning into and out of the minigame scene.
    /// Sets up and shuts down relevant systems.
    /// </summary>
    public class DesignTransitionSystem : SharedStateSystemBehaviour<DesignTransitionState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
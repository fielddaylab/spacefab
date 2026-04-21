using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Manages the player's current attempt (from timer start to timer end)
    /// </summary>
    public class AttemptSystem : SharedStateSystemBehaviour<SequenceState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
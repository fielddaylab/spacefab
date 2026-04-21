using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Facilitates displaying the results after evaluating the player's design.
    /// </summary>
    public class ResultSystem : SharedStateSystemBehaviour<ResultState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
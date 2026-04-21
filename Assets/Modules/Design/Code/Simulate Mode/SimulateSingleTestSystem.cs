using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages running through a single test in the test suite.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhase.Update, 2, UpdateMasks.SimulateModeMask)]
    public class SimulateSingleTestSystem : SharedStateSystemBehaviour<SimulateUIState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
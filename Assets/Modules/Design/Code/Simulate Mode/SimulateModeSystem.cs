using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages the high-level features of Simulate mode.
    /// Delegates full test suite previews to SimulateMultiTestSystem.
    /// Delegates single test previews to SimualteSingleTestSystem.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhase.Update, 1, UpdateMasks.SimulateModeMask)]
    public class SimulateModeSystem : SharedStateSystemBehaviour<SimulateModeState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
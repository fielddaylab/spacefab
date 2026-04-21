using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages running through a sequence of tests in the test suite.
    /// Delegates to SimulateSingleTestSystem for each item in the multi-test sequence.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhase.Update, 3, UpdateMasks.SimulateModeMask)]
    public class SimulateMultiTestSystem : SharedStateSystemBehaviour<SimulateUIState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe SystemFunctionShim GetDelegate() {
            return new SystemFunctionShim(&ProcessWork);
        }
    }
}
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Runs through a single test in the test suite.
    /// Runs on Update at order 2 under SimulateModeMask. Currently a stub.
    /// </summary>
    public class SimulateSingleTestSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 2, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateUIState>()
            );
        }

        // TODO: implement single-test run.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Runs through a sequence of tests in the test suite, delegating each one to SimulateSingleTestSystem.
    /// Runs on Update at order 3 under SimulateModeMask. Currently a stub.
    /// </summary>
    public class SimulateMultiTestSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 3, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateUIState>()
            );
        }

        // TODO: implement multi-test sequencing.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

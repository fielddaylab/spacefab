using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Manages the high-level features of Simulate mode.
    /// Delegates full test suite previews to SimulateMultiTestSystem.
    /// Delegates single test previews to SimulateSingleTestSystem.
    /// Runs on Update at order 1 under SimulateModeMask. Currently a stub.
    /// </summary>
    public class SimulateModeSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 1, UpdateMasks.SimulateModeMask),
                new SysPermissions()
                    .ReadWriteShared<SimulateModeState>()
            );
        }

        // TODO: implement simulate-mode coordination.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

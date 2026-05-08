using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Manages the "Clear All" feature, removing all player-drawn additions to the grid.
    /// Runs on Update at order 12 under ToolModeMask. Currently a stub.
    /// </summary>
    public class ClearAllSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 12, UpdateMasks.ToolModeMask),
                new SysPermissions()
                    .ReadWriteShared<ToolModeState>()
                    .ReadWriteShared<GridStackState>()
            );
        }

        // TODO: implement clear-all behavior.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

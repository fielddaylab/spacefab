using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceFab.Design {
    /// <summary>
    /// Manages Tool mode, in which the player is actively shaping the grid.
    /// Runs on any Update phase at order 0 under ToolModeMask. Currently a stub.
    /// </summary>
    public class ToolModeSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.Update, 0, UpdateMasks.ToolModeMask),
                new SysPermissions()
                    .ReadWriteShared<ToolModeState>()
                    .ReadWriteShared<GridStackState>()
                    .ReadWriteShared<VisualGridStackState>()
            );
        }

        // TODO: implement tool-mode orchestration.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

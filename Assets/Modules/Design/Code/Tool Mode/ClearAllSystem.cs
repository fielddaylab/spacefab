using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Manages the "Clear All" feature, removing all player-drawn additions to the grid.
    /// Runs on Update at order 12 under ToolModeMask. Currently a stub.
    /// </summary>
    public class ClearAllSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 5, UpdateMasks.ToolModeMask),
                new SysPermissions()
                    .ReadWriteShared<ToolbarState>()
                    .ReadWriteShared<GridStackState>()
                    .ReadWriteShared<VisualGridStackState>()
            );
        }

        // TODO: implement clear-all behavior.
        static private void ProcessWork(float deltaTime)
        {
            ToolbarState toolbarState = Find.State<ToolbarState>();
            if (!toolbarState.ClearRequestedThisFrame) { return; }

            GridStackState gridStackState = Find.State<GridStackState>();
            GridStackUtility.ClearGridStack(gridStackState);

            VisualGridStackState visualGridStackState = Find.State<VisualGridStackState>();
            visualGridStackState.VisualsNeedRefreshing = true;
        }
    }
}

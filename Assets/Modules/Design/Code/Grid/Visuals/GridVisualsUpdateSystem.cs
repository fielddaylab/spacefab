using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals {
    /// <summary>
    /// Refreshes the visual grid's metal and transistor layers when the visuals state flags
    /// them dirty. Runs on LateUpdate at order 0 under DesignMask.
    /// </summary>
    public class GridVisualsUpdateSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.DesignMask),
                new SysPermissions()
                    .ReadWriteShared<VisualGridStackState>()
            );
        }

        // When visuals are marked dirty, re-renders both layers from the current grid data.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out VisualGridStackState visualState
                );

            var spriteDB = Find.GlobalAsset<GridSpriteDB>();

            if (visualState.VisualsNeedRefreshing) {
                if (visualState.VisualGridStack == null || visualState.VisualGridStack.GridLayers == null || visualState.VisualGridStack.GridLayers.Length == 0) {
                    Log.Warn("[GridVisualsUpdateSystem] Attempted to update grid visuals when visuals have not been initialized!");
                    return;
                }

                // Render Metal Layer
                visualState.VisualGridStack.GridLayers[0].RefreshAll(spriteDB);
                // Render Transistor Layer
                visualState.VisualGridStack.GridLayers[1].RefreshAll(spriteDB);

                Log.Msg("[ToolInteractSystem] refreshing visuals need refresh (to false)");
                visualState.VisualsNeedRefreshing = false;
            }
        }
    }
}

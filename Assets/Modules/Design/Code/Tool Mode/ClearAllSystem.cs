using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages the "Clear All" feature, removing all player-drawn additions to the grid.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhase.Update, 12, UpdateMasks.ToolModeMask)]
    public class ClearAllSystem : SharedStateSystemBehaviour<ToolModeState, GridStackState>
    {
        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }

        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }
    }
}
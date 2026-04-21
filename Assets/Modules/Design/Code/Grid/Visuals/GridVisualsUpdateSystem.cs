using BeauUtil.Debugger;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.LateUpdate,0, UpdateMasks.DesignMask)]
    public class GridVisualsUpdateSystem : SharedStateSystemBehaviour<VisualGridStackState, SpriteDB>
    {
		protected override unsafe SystemFunctionShim GetDelegate() {
			return new SystemFunctionShim(&ProcessWork);
		}

		static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

            if (m_StateA.VisualsNeedRefreshing)
            {
                if (m_StateA.VisualGridStack == null || m_StateA.VisualGridStack.GridLayers == null || m_StateA.VisualGridStack.GridLayers.Length == 0) {
                    Log.Warn("[GridVisualsUpdateSystem] Attempted to update grid visuals when visuals have not been initialized!");
                    return; 
                }

                // Render Metal Layer
                m_StateA.VisualGridStack.GridLayers[0].RefreshAll(ref m_StateB);
                // Render Transistor Layer
                m_StateA.VisualGridStack.GridLayers[1].RefreshAll(ref m_StateB);

                m_StateA.VisualsNeedRefreshing = false;
            }
        }
    }
}
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design.Visuals;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    [SysUpdate(FieldDay.GameLoopPhase.PreUpdate, 0, UpdateMasks.SetupMask)]
    public class GridStackLoadSystem : SharedStateSystemBehaviour<DesignTransitionState, GridStackState, DesignMinigameState, MinigameSaveStates, VisualGridStackState>
    {
		protected override unsafe SystemFunctionShim GetDelegate() {
			return new SystemFunctionShim(&ProcessWork);
		}

        static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

            switch (m_StateA.Phase)
            {
                
                case DesignTransitionPhase.SetupBaseLevel:
                    Debug.Log("[GridStackLoadSystem] Setting up base level...");
                    // TODO: load base level
                    VisualGridStackUtility.Init(ref m_StateE.VisualGridStack, m_StateB.GridStack.LayerDims.X, m_StateB.GridStack.LayerDims.Y, m_StateE.CellVisualsPrefab, m_StateE.CellVisualsContainer);
                    VisualGridStackUtility.RefreshGridSize(m_StateE.GridRenderer, m_StateB.GridStack.LayerDims.X, m_StateB.GridStack.LayerDims.Y);
                    m_StateA.Phase = DesignTransitionPhase.ApplySave;
                    break;
                case DesignTransitionPhase.ApplySave:
                    Debug.Log("[GridStackLoadSystem] Applying save to level...");
                    // TODO: apply save
                    m_StateA.Phase = DesignTransitionPhase.FinalizeLevel;
                    break;
                case DesignTransitionPhase.FinalizeLevel:
                    Debug.Log("[GridStackLoadSystem] Finalizing level...");
                    // TODO: finalize level (enforce eraseable)
                    // Update visuals to match grid state
                    m_StateE.VisualsNeedRefreshing = true;
                    m_StateA.Phase = DesignTransitionPhase.SetupComplete;
                    break;
                case DesignTransitionPhase.SetupComplete:
                    Debug.Log("[GridStackLoadSystem] Load Complete!");
                    GameLoop.SuspendUpdates(UpdateMasks.SetupMask);
                    break;
                default:
                    break;
            }
        }
    }
}
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design {
    [SysUpdate(FieldDay.GameLoopPhase.PreUpdate, 0, UpdateMasks.SetupMask)]
    public class GridStackLoadSystem : SharedStateSystemBehaviour<DesignTransitionState, GridStackState, DesignMinigameState, MinigameSaveStates>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase <= DesignTransitionPhase.SetupComplete;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                /*
                case DesignTransitionPhase.SetupBaseLevel:
                    Debug.Log("[GridStackLoadSystem] Setting up base level...");
                    // TODO: load base level
                    m_StateA.Phase = DesignTransitionPhase.ApplySave;
                    break;
                */
                case DesignTransitionPhase.ApplySave:
                    Debug.Log("[GridStackLoadSystem] Applying save to level...");
                    // TODO: apply save
                    m_StateA.Phase = DesignTransitionPhase.SetupComplete;
                    break;
                case DesignTransitionPhase.FinalizeLevel:
                    Debug.Log("[GridStackLoadSystem] Finalizing level...");
                    // TODO: finalize level (enforce eraseable)
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
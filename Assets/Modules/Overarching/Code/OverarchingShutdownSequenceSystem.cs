using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.ShutdownMask)]
    public class OverarchingShutdownSequenceSystem : SharedStateSystemBehaviour<OverarchingShutdownSequenceState, MinigameZonesState, ChapterState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != OverarchingShutdownPhase.Waiting && m_StateA.Phase != OverarchingShutdownPhase.ShutdownComplete;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case OverarchingShutdownPhase.ShuttingDown:
                    // unload available contracts
                    Game.Assets.UnloadPackage(m_StateC.CurrAvailableContractAssetsPack);
                    m_StateA.Phase = OverarchingShutdownPhase.ShutdownComplete;

                    GameLoop.SuspendUpdates(Bits.All32);
                    GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
                    Game.Scenes.LoadMainScene(m_StateB.Zones[m_StateB.CurrSelectedIndex].MinigameScene);
                    Game.Events.Dispatch(GameEvents.OnMinigameLoad);
                    break;
                default:
                    break;
            }
        }
    }
}
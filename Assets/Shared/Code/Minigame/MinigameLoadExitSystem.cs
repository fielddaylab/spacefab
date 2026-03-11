using FieldDay;
using FieldDay.Systems;
using SpaceFab.Design;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.MinigameTransitionMask)]
    public class MinigameLoadExitSystem : SharedStateSystemBehaviour<MinigameLoadExitState, MinigameStateInterfacer, MinigameSaveStates, ReturnMenuState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && !m_StateA.Phase.Equals(MinigameLoadExitPhase.None);
        }

        public override void ProcessWork(float deltaTime)
        {
            switch (m_StateA.Phase)
            {
                case MinigameLoadExitPhase.Loading:
                    m_StateB.MinigameState.ImportState(m_StateC);
                    m_StateA.Phase = MinigameLoadExitPhase.Loaded;
                    break;
                case MinigameLoadExitPhase.Loaded:
                    GameLoop.ResumeUpdates(m_StateB.MinigameState.DefaultUpdateMask);
                    m_StateA.Phase = MinigameLoadExitPhase.None;
                    break;
                case MinigameLoadExitPhase.Exiting:
                    m_StateB.MinigameState.ExportState(ref m_StateC);
                    m_StateA.Phase = MinigameLoadExitPhase.Exited;
                    break;
                case MinigameLoadExitPhase.Exited:
                    //TODO: resume updates of overarching scene
                    Game.Events.Dispatch(GameEvents.OnMinigameExit);
                    Game.Scenes.LoadMainScene(m_StateD.ReturnScene);
                    m_StateA.Phase = MinigameLoadExitPhase.None;
                    break;
                default:
                    break;
            }
        }
    }
}
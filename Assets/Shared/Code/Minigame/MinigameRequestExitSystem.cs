using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 0)]
    public class MinigameRequestExitSystem : SharedStateSystemBehaviour<MinigameRequestExitState, MinigameLoadExitState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.ExitRequestState)
            {
                case RequestState.Requested:
                    break;
                case RequestState.Pending:
                    break;
                case RequestState.Confirmed:
                    // begin exit system
                    m_StateB.Phase = MinigameLoadExitPhase.Exiting;
                    GameLoop.SuspendUpdates(Bits.All32);
                    GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
                    m_StateA.ExitRequestState = RequestState.None;
                    break;
                case RequestState.None:
                default:
                    break;
            }
        }
    }
}
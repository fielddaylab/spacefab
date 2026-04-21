using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 10)]
    public class MinigameRequestExitSystem : SharedStateSystemBehaviour<MinigameRequestExitState, MinigameLoadExitState>
    {
        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }

        static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

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
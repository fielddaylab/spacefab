using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ContractSystemsMask)]
    public class ContractCompletionSystem : SharedStateSystemBehaviour<ContractCompletionState, ContractLayoutState, PlayerProgressState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != ContractCompletionPhase.Waiting;
        }
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            // TODO: implement
            switch (m_StateA.Phase)
            {
                case ContractCompletionPhase.Loading:
                    // ensure chapter load is complete
                    break;
                case ContractCompletionPhase.EnterPreviousContract:
                    break;
                case ContractCompletionPhase.EvaluatePreviousContract:
                    break;
                case ContractCompletionPhase.HidePreviousContract:
                    // TODO
                    // Complete
                    m_StateA.Phase = ContractCompletionPhase.Completed;
                    break;
                default:
                    break;
            }

            m_StateA.Phase = ContractCompletionPhase.Completed;
        }
    }
}
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ContractSystemsMask)]
    public class ContractSelectSystem : SharedStateSystemBehaviour<ContractSelectState, ContractLayoutState, PlayerProgressState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            // TODO

            m_StateA.Phase = ContractSelectPhase.Completed;
        }
    }
}
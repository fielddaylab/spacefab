using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, -10, UpdateMasks.ContractSystemsMask)]
    public class ContractConfirmSystem : SharedStateSystemBehaviour<ContractConfirmState, ContractSelectState, ContractLayoutState, ChapterState, ContractAssetsLookup>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractConfirmPhase.Confirming:
                    if (!m_StateA.ConfirmRoutine.Exists())
                    {
                        m_StateA.ConfirmRoutine.Replace(ContractConfirmUtility.ConfirmContractRoutine(m_StateA, m_StateB, m_StateC, m_StateD, m_StateE));
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
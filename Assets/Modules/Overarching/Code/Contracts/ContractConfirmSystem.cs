using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, -10, UpdateMasks.ContractSystemsMask)]
    public class ContractConfirmSystem : SharedStateSystemBehaviour<ContractConfirmState, ContractSelectState, ContractLayoutState, ChapterState, ContractAssetsLookup, SharedUIState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractConfirmPhase.Confirming:
                    ProcessConfirming();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        private void ProcessConfirming()
        {
            if (!m_StateA.ConfirmRoutine.Exists())
            {
                m_StateA.ConfirmRoutine.Replace(ContractConfirmUtility.ConfirmContractRoutine(m_StateA, m_StateB, m_StateC, m_StateD, m_StateE, m_StateF));
            }
        }

        #endregion // Helpers
    }
}
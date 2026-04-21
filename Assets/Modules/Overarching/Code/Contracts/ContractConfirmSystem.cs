using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, -9, UpdateMasks.ContractSystemsMask)]
    public class ContractConfirmSystem : SharedStateSystemBehaviour<ContractConfirmState, ContractSelectState, ContractLayoutState, ChapterState, ContractAssetsLookup, SharedUIState>
    {
		protected override unsafe SystemFunctionShim GetDelegate() {
			return new SystemFunctionShim(&ProcessWork);
		}

		static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

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

        static private void ProcessConfirming()
        {
            if (!m_StateA.ConfirmRoutine.Exists())
            {
                m_StateA.ConfirmRoutine.Replace(ContractConfirmUtility.ConfirmContractRoutine(m_StateA, m_StateB, m_StateC, m_StateD, m_StateE, m_StateF));
            }
        }

        #endregion // Helpers
    }
}
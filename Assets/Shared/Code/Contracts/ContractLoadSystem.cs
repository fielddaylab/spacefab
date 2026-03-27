using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ContractSystemsMask)]
    public class ContractLoadSystem : SharedStateSystemBehaviour<ContractLoadState, ContractAssetsLookup, ChapterState, ContractLayoutState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractLoadPhase.BeginLoad:
                    ProcessBeginLoad();
                    break;
                case ContractLoadPhase.Loading:
                    ProcessLoading();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        private void ProcessBeginLoad()
        {
            StringHash32 contractId = m_StateC.CurrAvailableContractsBundle.AvailableContracts[m_StateC.LastSelectedContractIndex].AssetId;
            m_StateA.LoadRoutine.Replace(ContractsLookupUtility.LoadContract(m_StateB, contractId));
            m_StateA.Phase = ContractLoadPhase.Loading;
        }

        private void ProcessLoading()
        {
            if (!m_StateA.LoadRoutine.Exists())
            {
                m_StateD.ViewCurrContractButton.gameObject.SetActive(true);
                m_StateA.Phase = ContractLoadPhase.Completed;
            }
        }



        #endregion // Helpers
    }
}
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, -10, UpdateMasks.ContractSystemsMask)]
    public class ContractSelectSystem : SharedStateSystemBehaviour<ContractSelectState, ContractLayoutState, ContractAssetsLookup, ChapterLoadState, ChapterState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractSelectPhase.Loading:
                    m_StateB.SelectionRoutine.Replace(ContractSelectUtility.PresentAvailableRoutine(m_StateA, m_StateB, m_StateE));
                    m_StateA.Phase = ContractSelectPhase.PresentAvailableContracts;
                    break;
                case ContractSelectPhase.PresentAvailableContracts:
                    if (!m_StateB.SelectionRoutine.Exists())
                    {
                        m_StateA.Phase = ContractSelectPhase.SelectContract;
                    }
                    break;
                case ContractSelectPhase.SelectContract:
                    if (m_StateA.SelectedContractIndex != -1 && m_StateB.ConfirmContractButton.interactable == false)
                    {
                        m_StateB.ConfirmContractButton.interactable = true;
                    }
                    if (m_StateA.SelectionConfirmed)
                    {
                        m_StateB.SelectionRoutine.Replace(ContractSelectUtility.ConfirmContractRoutine(m_StateA, m_StateB, m_StateE, m_StateC));
                        m_StateA.Phase = ContractSelectPhase.ConfirmContract;
                    }
                    break;
                case ContractSelectPhase.ConfirmContract:
                    if (!m_StateB.SelectionRoutine.Exists())
                    {
                        m_StateA.Phase = ContractSelectPhase.Completed;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
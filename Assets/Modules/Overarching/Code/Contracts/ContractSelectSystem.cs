using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, -10, UpdateMasks.ContractSystemsMask)]
    public class ContractSelectSystem : SharedStateSystemBehaviour<ContractSelectState, ContractLayoutState, ChapterState, PlayerProgressState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractSelectPhase.Loading:
                    m_StateB.SelectionRoutine.Replace(ContractSelectUtility.PresentAvailableRoutine(m_StateA, m_StateB, m_StateC, m_StateD));
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
                    if (m_StateA.SelectedContractIndexChanged)
                    {
                        ContractUtility.LoadContractData(m_StateB.SelectionContractUI, m_StateC.CurrAvailableContractsBundle.AvailableContracts[m_StateA.SelectedContractIndex]);
                        m_StateA.SelectedContractIndexChanged = false;
                    }
                    if (m_StateA.SelectionConfirmed)
                    {
                        m_StateC.LastSelectedContractIndex = m_StateA.SelectedContractIndex;
                        m_StateA.Phase = ContractSelectPhase.Completed;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
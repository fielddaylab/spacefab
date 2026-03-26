using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, -10, UpdateMasks.ContractSystemsMask)]
    public class ContractChangeSystem : SharedStateSystemBehaviour<ContractChangeState, ContractSelectState, ContractLayoutState, ContractAssetsLookup, ChapterLoadState, ChapterState, ContractConfirmState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractChangePhase.Starting:
                    Debug.Log("[ContractChangeSystem] Starting");
                    m_StateB.Phase = ContractSelectPhase.Waiting;
                    m_StateA.ChangeDoubleConfirmed = false;
                    m_StateA.TransitionRoutine.Replace(ContractChangeUtility.ViewCurrentRoutine(m_StateA, m_StateB, m_StateC, m_StateF));
                    m_StateA.Phase = ContractChangePhase.Viewing;
                    break;
                case ContractChangePhase.Viewing:
                    break;
                case ContractChangePhase.ContractSelectSystem:
                    if (m_StateB.Phase == ContractSelectPhase.Waiting)
                    {
                        m_StateA.StashedSelectedContractIndex = m_StateB.SelectedContractIndex;
                        m_StateB.Phase = ContractSelectPhase.Loading;
                        m_StateG.Phase = ContractConfirmPhase.Waiting;
                        m_StateC.HideCurrContractButton.gameObject.SetActive(false);
                        Debug.Log("[ContractChangeSystem] Deferring to ContractSelectSystem");
                    }
                    else if (m_StateB.Phase == ContractSelectPhase.Completed)
                    {
                        if (m_StateB.SelectedContractIndex == m_StateF.LastSelectedContractIndex)
                        {
                            // no change
                            m_StateA.Phase = ContractChangePhase.Docking;
                        }
                        else
                        {
                            m_StateC.DoubleConfirmCanvasGroup.blocksRaycasts = true;
                            m_StateC.DoubleConfirmCanvasGroup.alpha = 1;
                            Debug.Log("[ContractChangeSystem] Double Confirming Change");
                            m_StateA.Phase = ContractChangePhase.DoubleConfirmContract;
                        }
                    }
                    break;
                case ContractChangePhase.DoubleConfirmContract:
                    if (m_StateA.ChangeDoubleConfirmed)
                    {
                        m_StateA.Phase = ContractChangePhase.ContractConfirmSystem;
                        m_StateG.Phase = ContractConfirmPhase.Waiting;
                    }
                    break;
                case ContractChangePhase.DoubleCancelContract:
                    if (!m_StateA.TransitionRoutine.Exists())
                    {
                        m_StateA.TransitionRoutine.Replace(ContractChangeUtility.CancelChangeRoutine(m_StateA, m_StateB, m_StateC));
                        Debug.Log("[ContractChangeSystem] Canceling Change");
                    }
                    break;
                case ContractChangePhase.ContractConfirmSystem:
                    if (m_StateG.Phase == ContractConfirmPhase.Waiting)
                    {
                        Debug.Log("[ContractChangeSystem] Deferring to ContractConfirmSystem");
                        m_StateG.Phase = ContractConfirmPhase.Confirming;

                    }
                    else if (m_StateG.Phase == ContractConfirmPhase.Completed)
                    {
                        Debug.Log("[ContractChangeSystem] ContractConfirmSystem completed");
                        m_StateA.Phase = ContractChangePhase.Docking;
                    }
                    break;
                case ContractChangePhase.Docking:
                    Debug.Log("[ContractChangeSystem] Docking Contract");
                    if (!m_StateA.TransitionRoutine.Exists())
                    {
                        m_StateA.TransitionRoutine.Replace(ContractChangeUtility.DockContractRoutine(m_StateA, m_StateC));
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
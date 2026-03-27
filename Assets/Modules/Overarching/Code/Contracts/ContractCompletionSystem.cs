using BeauRoutine;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ContractSystemsMask)]
    public class ContractCompletionSystem : SharedStateSystemBehaviour<ContractCompletionState, ContractLayoutState, PlayerProgressState, ChapterState, AvailableContractsLookup>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != ContractCompletionPhase.Waiting;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ContractCompletionPhase.BeginLoadFromPrevChapter:
                    ProcessBeginLoadFromPrevChapter();
                    break;
                case ContractCompletionPhase.LoadFromPrevChapter:
                    ProcessLoadFromPrevChapter();
                    break;
                case ContractCompletionPhase.EnterPreviousContract:
                    ProcessEnterPrevContract();
                    break;
                case ContractCompletionPhase.EvaluatePreviousContract:
                    ProcessEvaluatePrevContract();
                    break;
                case ContractCompletionPhase.HidePreviousContract:
                    ProcessHidePrevContract();
                    break;
                case ContractCompletionPhase.UnloadFromPrevChapter:
                    ProcessUnloadFromPrevChapter();
                    break;
                default:
                    break;
            }

        }

        #region Helpers

        private void ProcessBeginLoadFromPrevChapter()
        {
            m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.LoadFromPrevChapterRoutine(m_StateA, m_StateD, m_StateE));
            m_StateA.Phase = ContractCompletionPhase.LoadFromPrevChapter;
        }

        private void ProcessLoadFromPrevChapter()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                ContractCompletionUtility.PopulateContractUI(m_StateA, m_StateB, m_StateD, m_StateE);
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.EnterPreviousRoutine(m_StateB));
                m_StateA.Phase = ContractCompletionPhase.EnterPreviousContract;
            }
        }

        private void ProcessEnterPrevContract()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.EvaluatePreviousRoutine(m_StateB));
                m_StateA.Phase = ContractCompletionPhase.EvaluatePreviousContract;
            }
        }

        private void ProcessEvaluatePrevContract()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.HidePreviousRoutine(m_StateB));
                m_StateA.Phase = ContractCompletionPhase.HidePreviousContract;
            }
        }

        private void ProcessHidePrevContract()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                m_StateD.LastSelectedContractIndex = -1;
                // Unload
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.UnloadFromPrevChapterRoutine(m_StateA, m_StateD, m_StateE));
                m_StateA.Phase = ContractCompletionPhase.UnloadFromPrevChapter;
            }
        }

        private void ProcessUnloadFromPrevChapter()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                // Complete
                m_StateA.Phase = ContractCompletionPhase.Completed;
            }
        }

        #endregion // Helpers
    }
}
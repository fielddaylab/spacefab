using BeauRoutine;
using FieldDay;
using FieldDay.Systems;
using SpaceFab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 10, UpdateMasks.ContractSystemsMask)]
    public class ContractCompletionSystem : SharedStateSystemBehaviour<ContractCompletionState, ContractLayoutState, PlayerProgressState, ChapterState, AvailableContractsLookup>
    {
        protected override unsafe SystemFunctionShim GetDelegate() {
            return new SystemFunctionShim(&ProcessWork);
        }

        static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

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

        static private void ProcessBeginLoadFromPrevChapter()
        {
            m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.LoadFromPrevChapterRoutine(m_StateA, m_StateD, m_StateE));
            m_StateA.Phase = ContractCompletionPhase.LoadFromPrevChapter;
        }

        static private void ProcessLoadFromPrevChapter()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                ContractCompletionUtility.PopulateContractUI(m_StateA, m_StateB, m_StateD, m_StateE);
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.EnterPreviousRoutine(m_StateB));
                m_StateA.Phase = ContractCompletionPhase.EnterPreviousContract;
            }
        }

        static private void ProcessEnterPrevContract()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.EvaluatePreviousRoutine(m_StateB));
                m_StateA.Phase = ContractCompletionPhase.EvaluatePreviousContract;
            }
        }

        static private void ProcessEvaluatePrevContract()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.HidePreviousRoutine(m_StateB));
                m_StateA.Phase = ContractCompletionPhase.HidePreviousContract;
            }
        }

        static private void ProcessHidePrevContract()
        {
            if (!m_StateB.CompletionRoutine.Exists())
            {
                m_StateD.LastSelectedContractIndex = -1;
                // Unload
                m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.UnloadFromPrevChapterRoutine(m_StateA, m_StateD, m_StateE));
                m_StateA.Phase = ContractCompletionPhase.UnloadFromPrevChapter;
            }
        }

        static private void ProcessUnloadFromPrevChapter()
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
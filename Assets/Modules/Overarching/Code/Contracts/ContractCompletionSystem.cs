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
    public class ContractCompletionSystem : SharedStateSystemBehaviour<ContractCompletionState, ContractLayoutState, PlayerProgressState, ChapterState>
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
                    m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.LoadFromPrevChapterRoutine());
                    m_StateA.Phase = ContractCompletionPhase.LoadFromPrevChapter;
                    break;
                case ContractCompletionPhase.LoadFromPrevChapter:
                    if (!m_StateB.CompletionRoutine.Exists())
                    {
                        m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.EnterPreviousRoutine(m_StateB));
                        m_StateA.Phase = ContractCompletionPhase.EnterPreviousContract;
                    }
                    break;
                case ContractCompletionPhase.EnterPreviousContract:
                    if (!m_StateB.CompletionRoutine.Exists()) {
                        m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.EvaluatePreviousRoutine(m_StateB));
                        m_StateA.Phase = ContractCompletionPhase.EvaluatePreviousContract;
                    }
                    break;
                case ContractCompletionPhase.EvaluatePreviousContract:
                    if (!m_StateB.CompletionRoutine.Exists())
                    {
                        m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.HidePreviousRoutine(m_StateB));
                        m_StateA.Phase = ContractCompletionPhase.HidePreviousContract;
                    }
                    break;
                case ContractCompletionPhase.HidePreviousContract:
                    if (!m_StateB.CompletionRoutine.Exists())
                    {
                        // Unload
                        m_StateB.CompletionRoutine.Replace(ContractCompletionUtility.UnloadFromPrevChapterRoutine());
                        m_StateA.Phase = ContractCompletionPhase.UnloadFromPrevChapter;
                    }
                    break;
                case ContractCompletionPhase.UnloadFromPrevChapter:
                    if (!m_StateB.CompletionRoutine.Exists())
                    {
                        // Complete
                        m_StateA.Phase = ContractCompletionPhase.Completed;
                    }
                    break;
                default:
                    break;
            }

        }
    }
}
using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    /// <summary>
    /// 1. Load the present chapter
    /// 2. Load current available contracts (in parallel with completion sequence)
    /// 2. If coming from previous chapter:
    ///     b. Perform completion sequence (in parallel with loading available contracts)
    /// 3. If Selected Contract not selected yet:
    ///     a. Select contract sequence
    /// 4. Load Selected Contract
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.SetupMask)]
    public class OverarchingStartupSequenceSystem : SharedStateSystemBehaviour<OverarchingStartupSequenceState, ChapterLoadState, ContractCompletionState, ContractSelectState, ChapterState, ContractLoadState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != OverarchingStartupSequencePhase.Completed && !Find.State<SharedUIState>().IsLoading;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case OverarchingStartupSequencePhase.LoadCurrChapter:
                    ProcessLoadCurrChapter();
                    break;
                case OverarchingStartupSequencePhase.LoadCurrAvailableContracts:
                    //start loading before contract completion routine, must complete before contract select system
                    ProcessLoadCurrAvailableContracts();
                    break;
                case OverarchingStartupSequencePhase.ContractCompletionSystem:
                    ProcessContractCompletion();
                    break;
                case OverarchingStartupSequencePhase.ContractSelectSystem:
                    ProcessContractSelectSystem();
                    break;
                case OverarchingStartupSequencePhase.LoadSelectedContract:
                    ProcessLoadSelectedContract();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        private void ProcessLoadCurrChapter()
        {
            if (m_StateB.Phase == ChapterLoadPhase.Waiting)
            {
                // begin ChapterLoadSystem
                GameLoop.ResumeUpdates(UpdateMasks.ChapterMask);
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ChapterLoadSystem");
                m_StateB.Phase = ChapterLoadPhase.LoadingChapter;
            }
            else
            {
                if (m_StateB.Phase == ChapterLoadPhase.Completed) 
                { 
                    // Move to next phase after chapter load
                    m_StateA.Phase = OverarchingStartupSequencePhase.LoadCurrAvailableContracts;
                }
            }
        }

        private void ProcessLoadCurrAvailableContracts()
        {
            // start load available contracts
            m_StateB.Phase = ChapterLoadPhase.LoadingAvailableContracts;

            // determine if contract completion is next
            var progress = Find.State<PlayerProgressState>();
            if (progress.RecentlyCompletedChapter)
            {
                m_StateA.Phase = OverarchingStartupSequencePhase.ContractCompletionSystem;
                m_StateC.Phase = ContractCompletionPhase.Waiting;
            }
            else
            {
                // skip contract completion
                MoveToContractSelect();
            }

            GameLoop.ResumeUpdates(UpdateMasks.ContractSystemsMask);
        }

        private void ProcessContractCompletion()
        {
            if (m_StateC.Phase == ContractCompletionPhase.Waiting)
            {
                // begin ChapterCompletionSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractCompletionSystem");

                m_StateC.Phase = ContractCompletionPhase.BeginLoadFromPrevChapter;
            }
            else
            {
                if (m_StateC.Phase == ContractCompletionPhase.Completed)
                {
                    MoveToContractSelect();
                }
            }
        }

        private void MoveToContractSelect()
        {
            // determine if contract selection must happen next
            if (m_StateE.LastSelectedContractIndex == -1)
            {
                m_StateA.Phase = OverarchingStartupSequencePhase.ContractSelectSystem;
                m_StateD.Phase = ContractSelectPhase.Waiting;
            }
            else
            {
                // load selected contract
                m_StateA.Phase = OverarchingStartupSequencePhase.LoadSelectedContract;
            }
        }

        private void ProcessContractSelectSystem()
        {
            // wait for LoadAvailableContracts routine to complete
            if (m_StateB.LoadRoutine.Exists() || m_StateB.Phase == ChapterLoadPhase.LoadingAvailableContracts) { return; }

            if (m_StateD.Phase == ContractSelectPhase.Waiting)
            {
                // begin ChapterCompletionSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractSelectSystem");
                m_StateD.Phase = ContractSelectPhase.Loading;
            }
            else
            {
                if (m_StateD.Phase == ContractSelectPhase.Completed)
                {
                    // load selected contract
                    m_StateF.Phase = ContractLoadPhase.Waiting;
                    m_StateA.Phase = OverarchingStartupSequencePhase.LoadSelectedContract;
                }
            }
        }

        private void ProcessLoadSelectedContract()
        {
            if (m_StateB.LoadRoutine.Exists() || m_StateB.Phase == ChapterLoadPhase.LoadingAvailableContracts) { return; }

            if (m_StateF.Phase == ContractLoadPhase.Waiting)
            {
                // begin ContractLoadSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractLoadSystem");
                m_StateF.Phase = ContractLoadPhase.BeginLoad;
                GameLoop.SuspendUpdates(UpdateMasks.ChapterMask);
            }
            else
            {
                if (m_StateF.Phase == ContractLoadPhase.Completed)
                {
                    Complete();
                    GameLoop.SuspendUpdates(UpdateMasks.ContractSystemsMask);
                }
            }
        }

        private void Complete()
        {
            m_StateA.Phase = OverarchingStartupSequencePhase.Completed;
            GameLoop.ResumeUpdates(UpdateMasks.OverarchingMask);
            Debug.Log("[OverarchingStartupSequenceSystem] Overarching Startup Sequence Completed");
        }

        #endregion // Helpers
    }
}
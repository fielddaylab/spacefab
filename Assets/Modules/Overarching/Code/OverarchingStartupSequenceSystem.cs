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
    public class OverarchingStartupSequenceSystem : SharedStateSystemBehaviour<OverarchingStartupSequenceState, ChapterLoadState, ContractCompletionState, ContractSelectState, ChapterState>
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
            if (m_StateE.LastSelectedContractIndex == -1 || progress.RecentlyCompletedLevel)
            {
                // next contract must be selected
                m_StateA.Phase = OverarchingStartupSequencePhase.ContractCompletionSystem;
                m_StateC.Phase = ContractCompletionPhase.Waiting;
            }
            else
            {
                // skip contract completion/selection
                // TODO: determine if contract selection must happen next
                bool isSelected = false;
                if (!isSelected)
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
        }

        private void ProcessContractCompletion()
        {

        }


        private void ProcessContractSelectSystem()
        {
            // wait for LoadAvailableContracts routine to complete
            if (m_StateB.LoadRoutine.Exists()) { return; }


        }

        private void ProcessLoadSelectedContract()
        {
            m_StateA.Phase = OverarchingStartupSequencePhase.Completed;
        }

        /*
        private void ProcessDetermineSequence()
        {
            // if player has no contract OR player just completed a level, trigger startup sequence
            var progress = Find.State<PlayerProgressState>();
            if (m_StateE.LastSelectedContractIndex == -1 || progress.RecentlyCompletedLevel)
            {
                // next contract must be selected
                m_StateA.Phase = OverarchingStartupSequencePhase.ChapterLoad;
                m_StateA.CompleteAfterLoad = false;
            }
            else
            {
                // skip contract completion/selection
                m_StateA.Phase = OverarchingStartupSequencePhase.ChapterLoad;
                m_StateA.CompleteAfterLoad = true;
            }
        }

        private void ProcessChapterLoad()
        {
            if (m_StateB.Phase == ChapterLoadPhase.Waiting)
            {
                // begin ChapterLoadSystem
                GameLoop.ResumeUpdates(UpdateMasks.ChapterMask);
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ChapterLoadSystem");
                m_StateB.Phase = ChapterLoadPhase.Loading;
            }
            else
            {
                // wait for ContractCompletionSystem to complete
                if (m_StateB.Phase == ChapterLoadPhase.Completed)
                {
                    if (m_StateA.CompleteAfterLoad)
                    {
                        LoadAvailableContracts();
                        Complete();
                    }
                    else
                    {
                        // Move to next phase after chapter load
                        m_StateA.Phase = OverarchingStartupSequencePhase.ContractCompletionSystem;
                        m_StateC.Phase = ContractCompletionPhase.Waiting;
                    }
                }
            }
        }

        private void ProcessContractCompletion()
        {
            if (m_StateC.Phase == ContractCompletionPhase.Waiting)
            {
                // begin ContractCompletionSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractCompletionSystem");
                GameLoop.SuspendUpdates(UpdateMasks.ChapterMask);
                GameLoop.ResumeUpdates(UpdateMasks.ContractSystemsMask);
                m_StateC.Phase = ContractCompletionPhase.Loading;
                m_StateD.Phase = ContractSelectPhase.Waiting;
            }
            else
            {
                // wait for ContractCompletionSystem to complete
                if (m_StateC.Phase == ContractCompletionPhase.Completed)
                {
                    m_StateA.Phase = OverarchingStartupSequencePhase.ContractSelectSystem;
                }
            }
        }

        private void ProcessContractSelect()
        {
            if (m_StateD.Phase == ContractSelectPhase.Waiting)
            {
                LoadAvailableContracts();
                // begin ContractSelectSystem
                Debug.Log("[OverarchingStartupSequenceSystem] Begin ContractSelectSystem");
                m_StateC.Phase = ContractCompletionPhase.Waiting;
                m_StateD.Phase = ContractSelectPhase.Loading;
            }
            else
            {
                // wait for ContractSelectSystem to complete
                if (m_StateD.Phase == ContractSelectPhase.Completed)
                {
                    Complete();
                    GameLoop.SuspendUpdates(UpdateMasks.ContractSystemsMask);
                }
            }
        }

        private void LoadAvailableContracts()
        {
            ChapterLoadUtility.LoadAvailableContracts(m_StateE);
        }

        private void Complete()
        {
            m_StateA.Phase = OverarchingStartupSequencePhase.Completed;
            GameLoop.ResumeUpdates(UpdateMasks.OverarchingMask);
            Debug.Log("[OverarchingStartupSequenceSystem] Overarching Startup Sequence Completed");
        }
        */

        #endregion // Helpers
    }
}
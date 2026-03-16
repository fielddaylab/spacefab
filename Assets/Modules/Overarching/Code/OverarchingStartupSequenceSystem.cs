using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.OverarchingMask)]
    public class OverarchingStartupSequenceSystem : SharedStateSystemBehaviour<OverarchingStartupSequenceState, ChapterLoadState, ContractCompletionState, ContractSelectState, ChapterState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != OverarchingStartupSequencePhase.Completed;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case OverarchingStartupSequencePhase.DetermineSequence:
                    ProcessDetermineSequence();
                    break;
                case OverarchingStartupSequencePhase.ChapterLoad:
                    ProcessChapterLoad();
                    break;
                case OverarchingStartupSequencePhase.ContractCompletionSystem:
                    ProcessContractCompletion();
                    break;
                case OverarchingStartupSequencePhase.ContractSelectSystem:
                    ProcessContractSelect();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        private void ProcessDetermineSequence()
        {
            // if player has no contract OR player just completed a level, trigger startup sequence
            var progress = Find.State<PlayerProgressState>();
            if (progress.LastSelectedContract.Equals(StringHash32.Null) || progress.RecentlyCompletedLevel)
            {
                m_StateA.Phase = OverarchingStartupSequencePhase.ChapterLoad;
                m_StateB.Phase = ChapterLoadPhase.Waiting;
            }
            else
            {
                LoadAvailableContracts();
                Complete();
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
                    // Move to next phase after chapter load
                    m_StateA.Phase = OverarchingStartupSequencePhase.ContractCompletionSystem;
                    m_StateC.Phase = ContractCompletionPhase.Waiting;
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
            // loaded whenever entering overarching scene (TODO: unload on exit overarching scene)
            Game.Assets.LoadPackage(m_StateE.CurrAvailableContractAssetsPack);
            m_StateE.CurrAvailableContractsBundle = Find.NamedAsset<ContractsBundle>("ContractsBundle");
        }

        private void Complete()
        {
            m_StateA.Phase = OverarchingStartupSequencePhase.Completed;
            Debug.Log("[OverarchingStartupSequenceSystem] Overarching Startup Sequence Completed");
        }

        #endregion // Helpers
    }
}
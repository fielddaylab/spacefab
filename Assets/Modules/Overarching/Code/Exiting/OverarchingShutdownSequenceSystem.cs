using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.ShutdownMask)]
    public class OverarchingShutdownSequenceSystem : SharedStateSystemBehaviour<OverarchingShutdownSequenceState, MinigameZonesState, ChapterState, AvailableContractsLookup>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != OverarchingShutdownPhase.Waiting && m_StateA.Phase != OverarchingShutdownPhase.ShutdownComplete;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case OverarchingShutdownPhase.BeginShutdown:
                    ProcessBeginShutdown();
                    break;
                case OverarchingShutdownPhase.ShuttingDown:
                    ProcessShuttingDown();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        private void ProcessBeginShutdown()
        {
            m_StateA.ShutdownRoutine.Replace(ContractsLookupUtility.UnloadAvailableContractsAtChapter(m_StateD, m_StateC, m_StateC.CurrChapterIndex));
            m_StateA.Phase = OverarchingShutdownPhase.ShuttingDown;
        }

        private void ProcessShuttingDown()
        {
            if (!m_StateA.ShutdownRoutine.Exists())
            {
                m_StateA.Phase = OverarchingShutdownPhase.ShutdownComplete;
            }
        }

        #endregion // Helpers
    }
}
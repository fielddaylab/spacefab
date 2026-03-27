using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.ShutdownMask)]
    public class OverarchingSubmitChapterSequenceSystem : SharedStateSystemBehaviour<OverarchingSubmitChapterSequenceState,
                                                                                     OverarchingShutdownSequenceState,
                                                                                     ChapterState,
                                                                                     PlayerProgressState>
    {
        public override bool HasWork()
        {
            return base.HasWork();
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case OverarchingSubmitChapterPhase.Starting:
                    ProcessStarting();
                    break;
                case OverarchingSubmitChapterPhase.ShutdownSequenceSystem:
                    ProcessShutdownSequenceSystem();
                    break;
                case OverarchingSubmitChapterPhase.MoveToNextChapter:
                    ProcessMoveToNextChapter();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        private void ProcessStarting()
        {
            m_StateB.Phase = OverarchingShutdownPhase.Waiting;
            m_StateA.Phase = OverarchingSubmitChapterPhase.ShutdownSequenceSystem;
        }

        private void ProcessShutdownSequenceSystem()
        {
            if (m_StateB.Phase == OverarchingShutdownPhase.Waiting)
            {
                // defer to ShutdownSequenceSystem
                m_StateB.Phase = OverarchingShutdownPhase.BeginShutdown;
            }
            else if (m_StateB.Phase == OverarchingShutdownPhase.ShutdownComplete)
            {
                m_StateA.Phase = OverarchingSubmitChapterPhase.MoveToNextChapter;
            }
        }

        private void ProcessMoveToNextChapter()
        {
            ChapterUtility.LoadNextChapter(m_StateC, m_StateD);
            m_StateA.Phase = OverarchingSubmitChapterPhase.TransitionComplete;
        }

        #endregion // Helpers
    }
}
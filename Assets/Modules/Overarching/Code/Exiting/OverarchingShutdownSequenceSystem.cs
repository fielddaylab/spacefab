using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.LateUpdate, 12, UpdateMasks.ShutdownMask)]
    public class OverarchingShutdownSequenceSystem : SharedStateSystemBehaviour<OverarchingShutdownSequenceState, MinigameZonesState, ChapterState, AvailableContractsLookup>
    {
		protected override unsafe SystemFunctionShim GetDelegate() {
			return new SystemFunctionShim(&ProcessWork);
		}

        static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

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

        static private void ProcessBeginShutdown()
        {
            m_StateA.ShutdownRoutine.Replace(ContractsLookupUtility.UnloadAvailableContractsAtChapter(m_StateD, m_StateC, m_StateC.CurrChapterIndex));
            m_StateA.Phase = OverarchingShutdownPhase.ShuttingDown;
        }

		static private void ProcessShuttingDown()
        {
            if (!m_StateA.ShutdownRoutine.Exists())
            {
                m_StateA.Phase = OverarchingShutdownPhase.ShutdownComplete;
            }
        }

        #endregion // Helpers
    }
}
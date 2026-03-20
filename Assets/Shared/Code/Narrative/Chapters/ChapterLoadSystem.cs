using FieldDay;
using FieldDay.Systems;
using SpaceFab.Overarching;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    /// <summary>
    /// Have array of AssetPacks in ChapterLoadState for each chapter
    /// Load AssetPack at index of curr chapter, convert to ChapterDef
    /// 
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ChapterMask)]
    public class ChapterLoadSystem : SharedStateSystemBehaviour<ChapterLoadState, ChapterState, PlayerProgressState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != ChapterLoadPhase.Waiting;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ChapterLoadPhase.Loading:
                    if (m_StateC.RecentlyCompletedLevel) {
                        // ChapterUtility.LoadPreviousState(m_StateB);
                        ChapterUtility.MoveFromPreviousState(m_StateB);
                    }
                    ChapterUtility.LoadCurrState(m_StateB, m_StateA, m_StateC);
                    // Complete
                    m_StateA.Phase = ChapterLoadPhase.Completed;
                    break;
                default:
                    break;
            }

            m_StateA.Phase = ChapterLoadPhase.Completed;
        }

        #region Helpers


        #endregion // Helpers
    }
}
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
    public class ChapterLoadSystem : SharedStateSystemBehaviour<ChapterLoadState, ChapterState, AvailableContractsLookup>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != ChapterLoadPhase.Waiting && m_StateA.Phase != ChapterLoadPhase.Completed;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            switch (m_StateA.Phase)
            {
                case ChapterLoadPhase.LoadingChapter:
                    if (!m_StateA.LoadRoutine.Exists())
                    {
                        // load curr chapter
                        m_StateA.LoadRoutine.Replace(ChapterLoadUtility.LoadCurrChapter(m_StateB, m_StateA));
                    }
                    break;
                case ChapterLoadPhase.LoadingAvailableContracts:
                    if (!m_StateA.LoadRoutine.Exists())
                    {
                        // load chapter in parallel with other systems
                        m_StateA.LoadRoutine.Replace(ChapterLoadUtility.LoadCurrAvailableContracts(m_StateB, m_StateA, m_StateC));
                    }
                    break;
                default:
                    break;
            }
        }

        #region Helpers


        #endregion // Helpers
    }
}
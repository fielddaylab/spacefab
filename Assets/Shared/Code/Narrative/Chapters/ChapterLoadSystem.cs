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
    public class ChapterLoadSystem : SharedStateSystemBehaviour<ChapterLoadState, ChapterState>
    {
        public override bool HasWork()
        {
            return base.HasWork() && m_StateA.Phase != ChapterLoadPhase.Waiting;
        }

        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            // TODO: implement
            switch (m_StateA.Phase)
            {
                case ChapterLoadPhase.Loading:
                    MoveFromPreviousState();
                    LoadNextState();
                    // Complete
                    m_StateA.Phase = ChapterLoadPhase.Completed;
                    break;
                default:
                    break;
            }

            m_StateA.Phase = ChapterLoadPhase.Completed;
        }

        #region Helpers

        private void MoveFromPreviousState()
        {
            m_StateB.PrevSelectedContractAssetPack = m_StateB.CurrSelectedContractAssetPack;

            if (m_StateB.CurrChapterAssetPack != null)
            {
                Game.Assets.UnloadPackage(m_StateB.CurrChapterAssetPack);
                // Unload PrevSelectedContractAsset AFTER ContractCompletionSystem
            }

            m_StateB.CurrChapterAssetPack = null;
            m_StateB.CurrAvailableContractAssetsPack = null;
            // m_StateA.CurrSelectedContractAssetPack = null;
        }

        private void LoadNextState()
        {
            // loaded until next chapter begins
            m_StateB.CurrChapterAssetPack = m_StateA.Chapters[m_StateB.CurrChapterIndex];
            Game.Assets.LoadPackage(m_StateB.CurrChapterAssetPack);
            m_StateB.CurrChapterDef = Find.NamedAsset<ChapterDef>("ChapterDef");

            // loaded whenever in overarching scene
            m_StateB.CurrAvailableContractAssetsPack = m_StateB.CurrChapterDef.AvailableContracts;
        }

        #endregion // Helpers
    }
}
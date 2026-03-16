using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.ContractSystemsMask)]
    public class ContractSelectSystem : SharedStateSystemBehaviour<ContractSelectState, ContractLayoutState, PlayerProgressState, ChapterLoadState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            // TODO
            switch (m_StateA.Phase)
            {
                case ContractSelectPhase.ConfirmContract:
                    // Game.Assets.LoadPackage(m_StateD.CurrAvailableContractAssets[0]);
                    break;
                default:
                    break;
            }

            m_StateA.Phase = ContractSelectPhase.Completed;
        }
    }
}
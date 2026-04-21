using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [SysUpdate(FieldDay.GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.ShutdownMask)]
    public class OverarchingToMinigameSequenceSystem : SharedStateSystemBehaviour<OverarchingToMinigameSequenceState, OverarchingShutdownSequenceState, MinigameZonesState, ChapterState, AvailableContractsLookup>
    {
		protected override unsafe SystemFunctionShim GetDelegate() {
			return new SystemFunctionShim(&ProcessWork);
		}

		static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

            switch (m_StateA.Phase)
            {
                case OverarchingToMinigamePhase.Starting:
                    ProcessStarting();
                    break;
                case OverarchingToMinigamePhase.ShutdownSequenceSystem:
                    ProcessShutdownSequenceSystem();
                    break;
                case OverarchingToMinigamePhase.TransitionToMinigame:
                    ProcessTransitionToMinigame();
                    break;
                default:
                    break;
            }
        }

        #region Helpers

        static private void ProcessStarting()
        {
            m_StateB.Phase = OverarchingShutdownPhase.Waiting;
            m_StateA.Phase = OverarchingToMinigamePhase.ShutdownSequenceSystem;
        }

        static private void ProcessShutdownSequenceSystem()
        {
            if (m_StateB.Phase == OverarchingShutdownPhase.Waiting)
            {
                // defer to ShutdownSequenceSystem
                m_StateB.Phase = OverarchingShutdownPhase.BeginShutdown;
            }
            else if (m_StateB.Phase == OverarchingShutdownPhase.ShutdownComplete)
            {
                m_StateA.Phase = OverarchingToMinigamePhase.TransitionToMinigame;
            }
        }

        static private void ProcessTransitionToMinigame()
        {
            GameLoop.SuspendUpdates(Bits.All32);
            GameLoop.ResumeUpdates(UpdateMasks.MinigameTransitionMask);
            Game.Scenes.LoadMainScene(m_StateC.Zones[m_StateC.CurrSelectedIndex].MinigameScene);
            Game.Events.Dispatch(GameEvents.OnMinigameLoad);
            m_StateA.Phase = OverarchingToMinigamePhase.TransitionComplete;
        }

        #endregion // Helpers
    }
}
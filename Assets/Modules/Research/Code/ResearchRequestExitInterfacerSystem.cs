using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research
{
    /// <summary>
    /// Manages what happens when the user requests to exit the Design minigame
    /// </summary>
    public class ResearchRequestExitInterfacerSystem : SharedStateSystemBehaviour<ResearchRequestExitInterfacerState, MinigameRequestExitState>
    {
		protected override unsafe delegate*<float, void> GetDelegate() {
			return &ProcessWork;
		}

		static private void ProcessWork(float deltaTime)
        {
            GetDependencies();

            if (m_StateB.ExitRequestState == RequestState.Requested)
            {
                /*
                m_StateA.ModalRoutine.Replace(RequestExitInterfacerUtility.ShowExitConfirmationModal(m_StateA.ExitConfirmationModal));

                m_StateB.ExitRequestState = RequestState.Pending;
                */
                m_StateB.ExitRequestState = RequestState.Confirmed;
            }
            else if (m_StateB.ExitRequestState == RequestState.Confirmed)
            {
                m_StateA.ModalRoutine.Replace(RequestExitInterfacerUtility.HideExitConfirmationModal(m_StateA.ExitConfirmationModal));
            }
        }
    }
}
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab
{
    /// <summary>
    /// Menu to return from Minigame to Overarching scene
    /// </summary>
    public class ReturnMenuState : SharedStateComponent, IRegistrationCallbacks
    {
        public DynamicButton ReturnButton;
        public SceneReference ReturnScene;

        public void OnRegister()
        {
            ReturnButton.onClick.AddListener(HandleReturnClicked);
        }

        public void OnDeregister()
        {
            ReturnButton.onClick.RemoveListener(HandleReturnClicked);
        }

        private void HandleReturnClicked()
        {
            ReturnUtility.OnReturnClicked(this);
        }
    }

    public static class ReturnUtility
    {
        public static void OnReturnClicked(ReturnMenuState state)
        {
            var pauseState = Find.State<PauseMenuState>();
            if (pauseState.GamePaused)
            {
                PauseUtility.StartTogglePause(pauseState);
            }

            var requestExitState = Find.State<MinigameRequestExitState>();
            requestExitState.ExitRequestState = RequestState.Requested;
        }
    }
}

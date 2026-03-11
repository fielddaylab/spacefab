using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
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
        public Button ReturnButton;
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
            Game.Scenes.LoadMainScene(state.ReturnScene);
        }
    }
}

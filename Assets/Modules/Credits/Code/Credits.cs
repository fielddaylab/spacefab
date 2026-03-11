using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Credits
{
    public class Credits : BatchedComponent, IScenePreload
    {
        [Header("Scene Management")]
        [SerializeField] private Button m_SkipButton;
        [SerializeField] private SceneReference m_ReturnScene;

        [Header("Credits")]
        [SerializeField] private Transform m_TextContainer;


        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            m_SkipButton.onClick.RemoveAllListeners();
            m_SkipButton.onClick.AddListener(HandleSkipClicked);

            return null;
        }

        private void Restart()
        {

        }

        private void HandleSkipClicked()
        {
            Game.Scenes.LoadMainScene(m_ReturnScene);
        }
    }
}
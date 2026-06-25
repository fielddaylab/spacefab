using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using NativeUtils;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Scenes;
using UnityEngine.SceneManagement;

namespace SpaceFab.Title
{
    public class FastBootController : SceneController
    {
        private enum ReadyPhase
        {
            Loading,
            AudioClick,
            Ready
        }

        [Header("Ready")]
        public TMP_Text PromptText;

        [Header("Run")]
        public AudioSource BootAudio;
        public CanvasGroup FadeGroup;

        [NonSerialized] private ReadyPhase m_ReadyPhase = 0;

        Routine m_PhaseRoutine;

        private void Awake()
        {
            NativeInput.OnMouseDown += OnNativeMouseDown;
            NativeInput.OnKeyDown += OnNativeKeyDown;
        }

        private void OnDestroy()
        {
            NativeInput.OnMouseDown -= OnNativeMouseDown;
            NativeInput.OnKeyDown -= OnNativeKeyDown;
        }

        private IEnumerator SwapToPrompt()
        {
            PromptText.gameObject.SetActive(true);
            PromptText.alpha = 0;
            yield return Routine.Combine(
                PromptText.FadeTo(1, 0.2f)
            );
        }

        private IEnumerator OnReady()
        {
            if (BootAudio != null) {
                yield return Routine.Combine(
                    BootAudio.WaitToComplete(),
                    FadeGroup.FadeTo(0, 1)
                    );
            }
            else {
                yield return FadeGroup.FadeTo(0, 1);
            }

            LoadNextScene();
        }

        protected override void OnSceneReady()
        {
            m_ReadyPhase = ReadyPhase.AudioClick;
            m_PhaseRoutine.Replace(this, SwapToPrompt());
        }

        #region Mouse Handler

        private void OnNativeMouseDown(float x, float y)
        {
            OnNativeDownCommon();
        }

        private void OnNativeKeyDown(KeyCode key)
        {
            OnNativeDownCommon();
        }

        private void OnNativeDownCommon()
        {
            if (m_ReadyPhase != ReadyPhase.AudioClick) {
                return;
            }

            if (BootAudio != null) {
                BootAudio.Play();
            }

            m_ReadyPhase = ReadyPhase.Ready;
            m_PhaseRoutine.Replace(this, OnReady());
        }

        #endregion // Mouse Handler

        private void LoadNextScene()
        {
            int buildIdx = SceneHelper.ActiveScene().BuildIndex + 1;
            SceneBinding nextScene = SceneHelper.FindSceneByIndex(buildIdx);
            Game.Scenes.LoadMainScene(nextScene);
        }
    }
}
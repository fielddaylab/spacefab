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
using FieldDay.Debugging;

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
        public Transform TitleTransform;
        public TMP_Text PromptText;

        [Header("Run")]
        public AudioSource BootAudio;
        public SceneReference NextScene;

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

        private IEnumerator PreloadNextScene() {
            Game.Scenes.QueueSceneFilePreload(NextScene);
            while(!Game.Scenes.AreQueuedSceneFilesReady()) {
                yield return null;
            }

            m_ReadyPhase = ReadyPhase.AudioClick;
            yield return SwapToPrompt();
        }

        private IEnumerator SwapToPrompt()
        {
            PromptText.gameObject.SetActive(true);
            PromptText.alpha = 0;
            
            yield return Routine.Combine(
                PromptText.FadeTo(1, 0.5f),
                TitleTransform.MoveTo(1.6f, 0.5f, Axis.Y, Space.Self).Ease(Curve.CubeOut),
                PromptText.transform.MoveTo(-2.5f, 0.5f, Axis.Y, Space.Self).Ease(Curve.CubeOut)
            );
        }

        private IEnumerator OnReady()
        {
            if (BootAudio != null) {
                BootAudio.Play();
                yield return BootAudio.WaitToComplete();
            }

            LoadNextScene();
        }

        protected override void OnSceneReady()
        {
            Routine.Start(this, PreloadNextScene());
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
            if (m_ReadyPhase != ReadyPhase.AudioClick || DebugFlags.IsConsoleOpen) {
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
            Game.Scenes.LoadMainScene(NextScene);
        }
    }
}
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

namespace Spacefab
{
    public class FastBootController : MonoBehaviour
    {
        static private readonly StringHash32 NextPreloadGroup = "Scene/Title";

        private enum ReadyPhase
        {
            Loading,
            AudioClick,
            Ready
        }

        [Header("Loading")]
        public TMP_Text LoadingText;
        public TMP_Text ErrorText;

        [Header("Ready")]
        public TMP_Text PromptText;

        [Header("Run")]
        public AudioSource BootAudio;
        public CanvasGroup FadeGroup;

        [NonSerialized] private ReadyPhase m_ReadyPhase = 0;
        [NonSerialized] private List<StreamingAssetHandle> m_AssetPrefetch = new List<StreamingAssetHandle>(8);
        private Routine m_LoadTimeWarning;

        private void Awake()
        {
            NativeInput.OnMouseDown += OnNativeMouseDown;
            NativeInput.OnKeyDown += OnNativeKeyDown;
            // Services.Assets.PreloadGroup(NextPreloadGroup);
        }

        private void Start()
        {
            int buildIdx = SceneHelper.ActiveScene().BuildIndex + 1;
            SceneBinding nextScene = SceneHelper.FindSceneByIndex(buildIdx);
            MainSceneTransitionArgs transition = new MainSceneTransitionArgs();
            Async.InvokeAsync(() => {
                Game.Scenes.LoadMainScene(nextScene, false, transition);
                Game.Scenes.RegisterTransitionHandlers(null, SceneLoadReady);
            });
        }

        private void OnDestroy()
        {
            NativeInput.OnMouseDown -= OnNativeMouseDown;
            NativeInput.OnKeyDown -= OnNativeKeyDown;
        }

        private IEnumerator SwapToPrompt()
        {
            yield return Routine.Combine(
                LoadingText.FadeTo(0, 0.2f)
            );
            LoadingText.gameObject.SetActive(false);

            // ReadyText.gameObject.SetActive(true);
            PromptText.gameObject.SetActive(true);
            // ReadyText.alpha = 0;
            PromptText.alpha = 0;
            yield return Routine.Combine(
                PromptText.FadeTo(1, 0.2f)
            // ReadyText.FadeTo(1, 0.2f)
            );
        }

        private IEnumerator FadeInError()
        {
            yield return 15;

            ErrorText.gameObject.SetActive(true);
            ErrorText.alpha = 0;
            yield return ErrorText.FadeTo(1, 0.2f);
        }

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
            // Log.Msg("native click at {0}, {1}", x, y);

            if (m_ReadyPhase != ReadyPhase.AudioClick)
            {
                return;
            }

            if (BootAudio != null)
            {
                BootAudio.Play();
            }

            m_ReadyPhase = ReadyPhase.Ready;
        }

        private IEnumerator SceneLoadReady(Scene scene, StringHash32 tag, MainSceneTransitionArgs transitionArgs)
        {
            // Services.Assets.StreamingPreloadGroup(NextPreloadGroup, m_AssetPrefetch);

            m_LoadTimeWarning.Replace(this, FadeInError());

            while (Streaming.IsLoading())
            {
                yield return 0.1f;
            }

            if (Streaming.ErrorCount() > 0)
            {
                while (Streaming.ErrorCount() > 0)
                {
                    Streaming.RetryErrored();
                    while (Streaming.IsLoading())
                    {
                        yield return 0.1f;
                    }
                }
            }

            /*
            while (!Services.Assets.PreloadGroupIsPrimaryLoaded(NextPreloadGroup))
            {
                yield return 0.1f;
            }
            */

            if (!ErrorText.gameObject.activeSelf)
            {
                m_LoadTimeWarning.Stop();
            }

            m_ReadyPhase = ReadyPhase.AudioClick;
            Routine.Start(this, SwapToPrompt());

            while (m_ReadyPhase < ReadyPhase.Ready)
            {
                yield return null;
            }

            // LoadingIcon.Queue();

            if (BootAudio != null)
            {
                yield return Routine.Combine(
                    BootAudio.WaitToComplete(),
                    FadeGroup.FadeTo(0, 1)
                    );
            }

            // Services.Assets.CancelStreamingPreloadGroup(m_AssetPrefetch);
        }
    }
}
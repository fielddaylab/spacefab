
using TMPro;
using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using FieldDay.UI;
using FieldDay.UI.Animation;
using FieldDay.Audio;
using FieldDay.SharedState;
using UnityEngine.Events;
using FieldDay.UI.Widgets;

namespace SpaceFab
{
    public class PauseMenuState : SharedStateComponent, IRegistrationCallbacks
    {
        #region Inspector

        public TMP_Text PlayerCodeDisplay;
        public SettingsMenu SettingsMenu;
        public BaseRaycasterInputLayer InputLayer;

        [Header("Button")]
        public GuiButton Button;
        public Image ButtonImage;
        public Sprite PauseSprite;
        public Sprite ResumeSprite;
        [Header("Fader")]
        public CanvasGroup Fader;
        public float TransitionTime;

        #endregion // Inspector

        [NonSerialized] public int StashedUpdateMask;
        [NonSerialized] public bool GamePaused;
        [NonSerialized] public Routine ButtonRoutine;
        [NonSerialized] public Routine FadeRoutine;

        public void OnRegister()
        {
            Button.OnClick.AddListener(HandleStartTogglePause);
            PlayerCodeDisplay.SetTextAndActive(PlayerPrefs.GetString("LatestPlayerCode", null));
        }

        public void OnDeregister()
        {
            Button.OnClick.RemoveListener(HandleStartTogglePause);

            FadeRoutine.Stop();
            ButtonRoutine.Stop();
        }

        private void HandleStartTogglePause()
        {
            PauseUtility.StartTogglePause(this);
        }
    }

    public static class PauseUtility
    {
        public static void SetPauseButtonActive(bool active, PauseMenuState state = null)
        {
            if (state == null) state = Find.State<PauseMenuState>();
            state.Button.Interactable = active;
            state.Button.gameObject.SetActive(active);
        }

        public static void StartTogglePause(PauseMenuState state)
        {
            if (state.GamePaused)
            {
                TogglePaused(state);
                state.FadeRoutine.Replace(FadeGroupOut(state));
                state.ButtonRoutine.Replace(SlideButtonOut(state));
            }
            else
            {
                state.FadeRoutine.Replace(FadeGroupIn(state));
                state.ButtonRoutine.Replace(SlideButtonIn(state))
                    .OnComplete(() => TogglePaused(state));
            }
        }

        public static void TogglePaused(PauseMenuState state)
        {
            SetPaused(state, !state.GamePaused);
        }

        private static void SetPaused(PauseMenuState state, bool paused)
        {
            if (Find.State<PauseMenuState>().GamePaused == paused)
            {
                return;
            }
            
            state.GamePaused = paused;

            if (paused)
            {
                SpacefabGame.Events.Dispatch(GameEvents.ClickPauseGame);
            }
            else
            {
                SpacefabGame.Events.Dispatch(GameEvents.ClickResumeGame);
            }

            Routine.Settings.Paused = paused;
            GameLoop.TimeScale = paused ? 0 : 1;
            Sfx.SetBusPaused(AudioBus.Master, paused);

            if (paused)
            {
                state.StashedUpdateMask = GameLoop.UpdateMask;
                GameLoop.SuspendUpdates(Bits.All32);
                GameLoop.ResumeUpdates(UpdateMasks.PauseUpdateMask);
                Game.Gui.PushPriority(state.InputLayer);
                Game.Events.Dispatch(GameEvents.OnGamePaused);
            }
            else
            {
                GameLoop.ResumeUpdates(state.StashedUpdateMask);
                Game.Gui.PopPriority(state.InputLayer);
                Game.Events.Dispatch(GameEvents.OnGameResumed);
            }
        }

        private static IEnumerator SlideButtonIn(PauseMenuState state)
        {
            state.Button.Interactable = false;
            yield return state.Button.transform.ScaleTo(1.5f, state.TransitionTime, Axis.XY).Ease(Curve.CubeIn);
            state.ButtonImage.sprite = state.ResumeSprite;
            state.Button.Interactable = true;
        }

        private static IEnumerator SlideButtonOut(PauseMenuState state)
        {
            state.Button.Interactable = false;
            yield return state.Button.transform.ScaleTo(1.0f, state.TransitionTime, Axis.XY).Ease(Curve.CubeIn);
            state.ButtonImage.sprite = state.PauseSprite;
            state.Button.Interactable = true;
        }

        private static IEnumerator FadeGroupOut(PauseMenuState state)
        {
            // Block input immediately so the fading-out panel can't be clicked
            state.Fader.interactable = false;
            state.Fader.blocksRaycasts = false;
            state.Fader.alpha = 1;
            yield return state.Fader.FadeTo(0, state.TransitionTime);
            state.Fader.gameObject.SetActive(false);
            state.Fader.alpha = 0;
        }

        private static IEnumerator FadeGroupIn(PauseMenuState state)
        {
            state.Fader.alpha = 0;
            state.Fader.gameObject.SetActive(true);
            yield return state.Fader.FadeTo(1, state.TransitionTime);
            state.Fader.alpha = 1;
            // Enable input once the panel is fully visible; the prefab authors the
            // CanvasGroup as non-interactive, so the settings controls stay dead
            // until we flip these on.
            state.Fader.interactable = true;
            state.Fader.blocksRaycasts = true;
        }
    }
}


using TMPro;
using System;
using BeauRoutine;
using FieldDay;
using UnityEngine;
using FieldDay.UI;
using FieldDay.Audio;
using FieldDay.UI.Widgets;
using SpaceFab.UI;
using FieldDay.Animation;
using UnityEngine.UI;

namespace SpaceFab
{
    public class PauseModule : BaseGuiModule, IRegistrationCallbacks
    {
        #region Inspector

        public Canvas Canvas;
        public BaseRaycasterInputLayer InputLayer;
        
        [Header("Buttons")]
        public GuiButton CloseButton;

        [Header("Display")]
        public LayoutOffset Offset;
        public CanvasGroup Fader;
        public SettingsMenu SettingsMenu;
        public TMP_Text PlayerCodeDisplay;

        #endregion // Inspector

        [NonSerialized] public int StashedUpdateMask;
        [NonSerialized] public bool GamePaused;
        [NonSerialized] public AnimHandle FadeRoutine;

        public void OnRegister()
        {
            PlayerCodeDisplay.SetTextAndActive(PlayerPrefs.GetString("LatestPlayerCode", null));
            CloseButton.OnClick.Register(() => PauseUtility.SetPaused(this, false));

            Canvas.enabled = false;
            InputLayer.SetInputOverride(false);
        }

        public void OnDeregister()
        {
            Game.Animation.CancelAnimation(ref FadeRoutine);
        }

        public sealed class OpenAnim : LiteAnimator<PauseModule> {
            public override void InitAnimation(PauseModule target, ref LiteAnimatorState state) {
                if (!target.Canvas.enabled) {
                    target.Canvas.enabled = true;
                    target.Fader.alpha = 0;
                }

                float alpha = target.Fader.alpha;
                state.Registers.A.Float() = alpha;
                state.ScaleTime(1 - alpha);

                target.InputLayer.SetInputOverride(false);
            }

            public override void ResetAnimation(PauseModule target, ref LiteAnimatorState state) {
            }

            public override void UpdateAnimation(PauseModule target, ref LiteAnimatorState state, float deltaTime) {
                target.Fader.alpha = Mathf.Lerp(state.Registers.A.Float(), 1, state.PercentProgress);
                if (state.IsLastFrame()) {
                    target.InputLayer.ClearInputOverride();
                }
            }

            static public readonly OpenAnim Instance = new OpenAnim();
        }

        public sealed class CloseAnim : LiteAnimator<PauseModule> {
            public override void InitAnimation(PauseModule target, ref LiteAnimatorState state) {
                float alpha = target.Fader.alpha;
                state.Registers.A.Float() = alpha;
                state.ScaleTime(alpha);

                target.InputLayer.SetInputOverride(false);
            }

            public override void ResetAnimation(PauseModule target, ref LiteAnimatorState state) {
            }

            public override void UpdateAnimation(PauseModule target, ref LiteAnimatorState state, float deltaTime) {
                target.Fader.alpha = state.Registers.A.Float() * state.PercentRemaining;
                if (state.IsLastFrame()) {
                    target.Canvas.enabled = false;
                }
            }

            static public readonly CloseAnim Instance = new CloseAnim();
        }
    }

    public static class PauseUtility
    {
        public static void SetPaused(PauseModule state, bool paused)
        {
            if (state.GamePaused == paused)
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
            Sfx.SetMixStateEnabled("PauseMix", paused, false);

            if (paused)
            {
                state.StashedUpdateMask = GameLoop.UpdateMask;
                GameLoop.SuspendUpdates(UpdateMasks.EntireGame);
                GameLoop.ResumeUpdates(UpdateMasks.PauseUpdateMask);
                PopupUtility.PushState();
                state.InputLayer.TryPushPriority();
                Game.Events.Dispatch(GameEvents.OnGamePaused);

                Game.Animation.CancelAnimation(ref state.FadeRoutine);
                state.FadeRoutine = Game.Animation.AddLiteAnimator(PauseModule.OpenAnim.Instance, state, 0.2f, GameLoopPhase.UnscaledUpdate);
            }
            else
            {
                GameLoop.ResumeUpdates(state.StashedUpdateMask);
                state.InputLayer.TryPopPriority();
                PopupUtility.PopState();
                Game.Events.Dispatch(GameEvents.OnGameResumed);

                Game.Animation.CancelAnimation(ref state.FadeRoutine);
                state.FadeRoutine = Game.Animation.AddLiteAnimator(PauseModule.CloseAnim.Instance, state, 0.2f, GameLoopPhase.UnscaledUpdate);
            }
        }
    }
}

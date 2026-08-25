using System;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Animation;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.UI {
    public sealed class PopupFaderControl : SharedStateComponent {
        public Canvas Canvas;
        public CanvasGroup Fade;
        public float TransitionDuration;

        [NonSerialized] public int StackDepth;
        [NonSerialized] public float CurrentAlpha;
        [NonSerialized] public AnimHandle CurrentAnim;

        private void Awake() {
            CurrentAlpha = 0;
            Canvas.enabled = false;
            Fade.alpha = 0;
        }

        /// <summary>
        /// A: float targetAlpha
        /// X: float startAlpha
        /// </summary>
        public sealed class FadeAnim : LiteAnimator<PopupFaderControl> {
            static public readonly FadeAnim Instance = new FadeAnim();

            public override void InitAnimation(PopupFaderControl target, ref LiteAnimatorState state) {
                state.Registers.X.Float() = target.CurrentAlpha;
            }

            public override void ResetAnimation(PopupFaderControl target, ref LiteAnimatorState state) {
            }

            public override void UpdateAnimation(PopupFaderControl target, ref LiteAnimatorState state, float deltaTime) {
                target.CurrentAlpha = Mathf.LerpUnclamped(state.Registers.X.Float(), state.Registers.A.Float(), state.PercentProgress);
                target.Canvas.enabled = target.CurrentAlpha > 0;
                target.Fade.alpha = target.CurrentAlpha;
            }
        }
    }

    static public partial class PopupUtility {
        static public void PushState() {
            Find.State(out PopupFaderControl fader);
            Assert.True(fader.StackDepth <= 0);
            if (fader.StackDepth++ == 0) {
                Game.Animation.CancelAnimation(ref fader.CurrentAnim);

                LiteAnimatorState animState = default;
                animState.ResetTime((1 - fader.CurrentAlpha) * fader.TransitionDuration);
                animState.Registers.A.Float() = 1;
                fader.CurrentAnim = Game.Animation.AddLiteAnimator(PopupFaderControl.FadeAnim.Instance, fader, animState, GameLoopPhase.UnscaledUpdate);
            }
        }

        static public void PopState() {
            Find.State(out PopupFaderControl fader);
            Assert.True(fader.StackDepth > 0, "Unbalanced PopupUtility.Push/PopState calls!");
            if (fader.StackDepth-- == 1) {
                Game.Animation.CancelAnimation(ref fader.CurrentAnim);

                LiteAnimatorState animState = default;
                animState.ResetTime(fader.CurrentAlpha * fader.TransitionDuration);
                animState.Registers.A.Float() = 0;
                fader.CurrentAnim = Game.Animation.AddLiteAnimator(PopupFaderControl.FadeAnim.Instance, fader, animState, GameLoopPhase.UnscaledUpdate);
            }
        }
    }
}
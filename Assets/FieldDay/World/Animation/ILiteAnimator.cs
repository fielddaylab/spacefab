using System;
using System.Runtime.CompilerServices;
using BeauRoutine;
using BeauUtil;
using FieldDay.Mathematics;

namespace FieldDay.Animation {
    public interface ILiteAnimator {
        void InitAnimation(object target, ref LiteAnimatorState state);
        void UpdateAnimation(object target, ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(object target, ref LiteAnimatorState state);
    }

    public interface ILiteAnimator<T> : ILiteAnimator where T : class {
        void InitAnimation(T target, ref LiteAnimatorState state);
        void UpdateAnimation(T target, ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(T target, ref LiteAnimatorState state);
    }

    public abstract class LiteAnimator<T> : ILiteAnimator<T> where T : class {
        public abstract void InitAnimation(T target, ref LiteAnimatorState state);

        public abstract void ResetAnimation(T target, ref LiteAnimatorState state);

        public abstract void UpdateAnimation(T target, ref LiteAnimatorState state, float deltaTime);

        void ILiteAnimator.InitAnimation(object target, ref LiteAnimatorState state) {
            InitAnimation(Unsafe.FastCast<T>(target), ref state);
        }

        void ILiteAnimator.ResetAnimation(object target, ref LiteAnimatorState state) {
            ResetAnimation(Unsafe.FastCast<T>(target), ref state);
        }

        void ILiteAnimator.UpdateAnimation(object target, ref LiteAnimatorState state, float deltaTime) {
            UpdateAnimation(Unsafe.FastCast<T>(target), ref state, deltaTime);
        }
    }

    public struct LiteAnimatorState {
        public float CurrentTime;
        public float Duration;
        public Curve Easing;
        public LiteAnimatorStateEvents Events;
        public LiteAnimatorRegisters Registers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetTime(float duration) {
            CurrentTime = 0;
            Duration = duration;
            Events = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetTimeWithDelay(float duration, float delay) {
            Duration = duration;
            CurrentTime = -delay;
            Events = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScaleTime(float scale) {
            CurrentTime *= scale;
            Duration *= scale;
        }

        /// <summary>
        /// Returns the percentage remaining of this animation.
        /// </summary>
        public float PercentRemaining {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 1f - Math.Min(1, Math.Max(0, CurrentTime / Duration)); }
        }

        /// <summary>
        /// Returns the percentage progress through the animation.
        /// </summary>
        public float PercentProgress {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Math.Min(1, Math.Max(0, CurrentTime / Duration)); }
        }

        /// <summary>
        /// Advances by the given number of seconds, and updates event flags.
        /// </summary>
        public void Advance(float deltaTime) {
            float prevTime = CurrentTime;
            CurrentTime += Math.Max(0, deltaTime);
            bool isFirstFrame = prevTime <= 0 && CurrentTime > 0;
            bool isLastFrame = prevTime < Duration && CurrentTime >= Duration;
            Events = (isFirstFrame ? LiteAnimatorStateEvents.FirstFrame : 0)
                | (isLastFrame ? LiteAnimatorStateEvents.LastFrame : 0);
        }

        /// <summary>
        /// Is this the first frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFirstFrame() {
            return (Events & LiteAnimatorStateEvents.FirstFrame) != 0;
        }

        /// <summary>
        /// Is this the last frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLastFrame() {
            return (Events & LiteAnimatorStateEvents.LastFrame) != 0;
        }

        /// <summary>
        /// Is this the last frame.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRunning() {
            return CurrentTime > 0;
        }

        /// <summary>
        /// Is this delayed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDelayed() {
            return CurrentTime < 0;
        }
    }

    public struct LiteAnimatorRegisters {
        public Vector128 A;
        public Vector128 X;
        public Vector128 Y;
    }

    [Flags]
    public enum LiteAnimatorStateEvents : byte {
        FirstFrame = 0x01,
        LastFrame = 0x04,
    }
}
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    /// <summary>
    /// Cycles a UI Image through a sprite sequence at a fixed rate. Frames and rate may be authored
    /// on the prefab for a self-contained looping graphic, or pushed at runtime by whatever binds
    /// the slot — the wiki's page illustration does the latter, one sequence per page.
    ///
    /// Fewer than two frames, or a non-positive rate, is a still image rather than a broken cycler:
    /// the first frame is applied and SpriteCycleSystem skips it from then on.
    ///
    /// Data only — every mutation routes through SpriteCyclerUtility, which also owns the
    /// "no frames means hide the slot" rule so callers don't each re-implement it.
    /// </summary>
    public class SpriteCycler : BatchedComponent, IRegistrationCallbacks {
        public Image Target;

        public Sprite[] Frames;
        public float FramesPerSecond = 0.5f;

        // Playback cursor
        [NonSerialized] public int FrameIndex;
        [NonSerialized] public float FrameTimer;

        public void OnRegister() {
            Assert.NotNullOrDestroyed(Target, "SpriteCycler.Target not authored on '{0}'", name);
            SpriteCyclerUtility.Rewind(this);
        }

        public void OnDeregister() {
        }
    }

    /// <summary>
    /// Logic paired with SpriteCycler. Advance is the per-frame step driven by SpriteCycleSystem;
    /// everything else is a bind-time call made by whoever owns the slot's contents.
    /// </summary>
    public static class SpriteCyclerUtility {
        // Binds a sequence and restarts it from the first frame. A null or empty sequence is valid
        // authoring — the slot hides rather than keeping the previous sequence's frame on screen.
        public static void SetFrames(SpriteCycler cycler, Sprite[] frames, float framesPerSecond) {
            Assert.NotNullOrDestroyed(cycler.Target, "SpriteCycler.Target not authored on '{0}'", cycler.name);

            cycler.Frames = frames;
            cycler.FramesPerSecond = framesPerSecond;
            Rewind(cycler);
            cycler.Target.gameObject.SetActive(HasFrames(cycler));
        }

        // Binds a single still sprite. Separate from SetFrames so callers with one sprite in hand
        // don't have to allocate a one-element array every bind.
        public static void SetSingleFrame(SpriteCycler cycler, Sprite frame) {
            Assert.NotNullOrDestroyed(cycler.Target, "SpriteCycler.Target not authored on '{0}'", cycler.name);

            cycler.Frames = null;
            cycler.FramesPerSecond = 0;
            cycler.FrameIndex = 0;
            cycler.FrameTimer = 0;
            cycler.Target.sprite = frame;
            cycler.Target.gameObject.SetActive(frame != null);
        }

        // Drops the sequence and hides the slot.
        public static void Clear(SpriteCycler cycler) {
            SetFrames(cycler, null, 0);
        }

        // Resets the playback cursor and applies the first frame, leaving the bound sequence alone.
        // Deliberately does not touch the target's active state — it runs during OnRegister, and
        // deactivating a GameObject mid-registration would re-enter the registry.
        public static void Rewind(SpriteCycler cycler) {
            cycler.FrameIndex = 0;
            cycler.FrameTimer = 0;

            if (HasFrames(cycler)) {
                cycler.Target.sprite = cycler.Frames[0];
            }
        }

        // Steps one cycler forward.
        public static void Advance(SpriteCycler cycler, float deltaTime) {
            // A one-frame or zero-rate cycler is a still image; nothing left to do after Rewind.
            if (!HasFrames(cycler) || cycler.Frames.Length < 2 || cycler.FramesPerSecond <= 0) { return; }

            cycler.FrameTimer += deltaTime;
            float frameDuration = 1f / cycler.FramesPerSecond;
            if (cycler.FrameTimer < frameDuration) { return; }

            // Advance by however many frame durations actually elapsed, not one per game frame, so
            // a hitch or a rate above the game's frame rate doesn't drag the cycle out of time.
            int step = (int) (cycler.FrameTimer / frameDuration);
            cycler.FrameTimer -= step * frameDuration;
            cycler.FrameIndex = (cycler.FrameIndex + step) % cycler.Frames.Length;
            cycler.Target.sprite = cycler.Frames[cycler.FrameIndex];
        }

        public static bool HasFrames(SpriteCycler cycler) {
            return cycler.Frames != null && cycler.Frames.Length > 0;
        }
    }
}

using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Onboarding {
    /// <summary>
    /// Owns the pool of pooled Highlight instances, the active id -> instance map, and the
    /// set of currently focus-locked tag ids. Lifetime is shared (lives in a long-lived
    /// onboarding scene); all mutation goes through OnboardingHighlightUtility.
    /// Prewarm happens in IScenePreload so the pool is ready before the first Leaf call.
    /// </summary>
    public class OnboardingHighlightState : SharedStateComponent, IRegistrationCallbacks, IScenePreload {
        [Serializable] public sealed class HighlightPool : SerializablePool<Highlight> { }

        [Header("Pool")]
        public HighlightPool Pool;

        [Header("Defaults")]
        // Margin (px for UI targets, world units for sprite targets) added on every side of
        // a target when a Leaf call does not specify a margin override. -1 from the Leaf
        // signature means "use this default".
        public float DefaultMargin = 8f;

        // Currently-shown highlights, indexed by the ElementTag id that summoned them.
        // Used to make HighlightElement idempotent on the same id and to resolve Release.
        [NonSerialized] public Dictionary<StringHash32, Highlight> ActiveById;

        // Subset of ActiveById whose highlight was summoned with lockFocus=true. Drives the
        // pre-raycast layer swap: while this set is non-empty, the input raycaster's event
        // mask is restricted to LayerMasks.TutorialFocus_Mask.
        [NonSerialized] public HashSet<StringHash32> LockedTagIds;

        // GameObject.GetInstanceID -> original layer index. Populated when a focus-locked
        // target's hierarchy is parked on the TutorialFocus layer; consumed when the lock
        // is released so each node returns to its original layer.
        [NonSerialized] public Dictionary<int, int> SavedLayersByInstanceId;

        public void OnRegister() {
            ActiveById = new Dictionary<StringHash32, Highlight>(8);
            LockedTagIds = new HashSet<StringHash32>();
            SavedLayersByInstanceId = new Dictionary<int, int>(32);
        }

        public void OnDeregister() {
            ActiveById?.Clear();
            LockedTagIds?.Clear();
            SavedLayersByInstanceId?.Clear();
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Pool.Prewarm();
            return null;
        }
    }
}

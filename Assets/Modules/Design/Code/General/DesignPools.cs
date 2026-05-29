using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Shared pools the Design minigame draws from. Today: just the per-input toggle overlay.
    /// Pattern mirrors ResearchPools and WikiChipPools — one nested SerializablePool subclass per
    /// payload type, an Active list for iteration, Prewarm in IScenePreload.
    /// </summary>
    public class DesignPools : SharedStateComponent, IScenePreload
    {
        [Serializable] public sealed class InputToggleVisualPool : SerializablePool<InputToggleVisual> { }

        [Header("Input Toggles")]
        public InputToggleVisualPool InputToggleOverlayPool;

        // Currently-allocated input-toggle overlays, grown / shrunk by
        // InputToggleUtility.SpawnInputOverlays on level entry. InputToggleSystem doesn't read
        // this — it walks Find.Components<InputToggleVisual>() instead — but the list is the
        // canonical "free these on next level load" set.
        [NonSerialized] public List<InputToggleVisual> ActiveInputToggleOverlays;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload()
        {
            InputToggleOverlayPool.Prewarm();
            ActiveInputToggleOverlays = new List<InputToggleVisual>(8);
            return null;
        }
    }
}

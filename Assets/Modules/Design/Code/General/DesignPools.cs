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
        [Serializable] public sealed class OutputTagVisualPool : SerializablePool<OutputTagVisual> { }

        [Header("Input Toggles")]
        public InputToggleVisualPool InputToggleOverlayPool;

        [Header("Output Tags")]
        public OutputTagVisualPool OutputTagOverlayPool;

        // Currently-allocated input-toggle overlays, grown / shrunk by
        // InputToggleUtility.SpawnInputOverlays on level entry. InputToggleSystem doesn't read
        // this — it walks Find.Components<InputToggleVisual>() instead — but the list is the
        // canonical "free these on next level load" set.
        [NonSerialized] public List<InputToggleVisual> ActiveInputToggleOverlays;

        // Currently-allocated output-tag overlays, mirrored on the input list above. The canonical
        // "free these on next level load" set for OutputTagUtility.
        [NonSerialized] public List<OutputTagVisual> ActiveOutputTagOverlays;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload()
        {
            InputToggleOverlayPool.Prewarm();
            OutputTagOverlayPool.Prewarm();
            ActiveInputToggleOverlays = new List<InputToggleVisual>(8);
            ActiveOutputTagOverlays = new List<OutputTagVisual>(8);
            return null;
        }
    }
}

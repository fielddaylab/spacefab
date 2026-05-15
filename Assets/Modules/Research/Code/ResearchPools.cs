using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Shared pools the Research minigame draws from. Owns the VFX pool
    /// pair used by the explosion sequence (bolt-zap flash + main
    /// burst), the pre-explode material the slot rig swaps to during
    /// that flash, and the pagination-dot pool used by the hypothesis
    /// panel. Pure data container plus an IScenePreload hook that
    /// prewarms each pool so the first allocation in a session does
    /// not incur instantiation cost.
    ///
    /// Add new pools here as more UI / VFX / per-frame-pooled objects
    /// land; the pattern is one nested SerializablePool subclass per
    /// payload type, an inspector field per usage, plus a Prewarm call
    /// in Preload.
    /// </summary>
    public class ResearchPools : SharedStateComponent, IScenePreload {
        [Serializable] public sealed class VfxPool : SerializablePool<ResearchVfxInstance> { }
        [Serializable] public sealed class DotPool : SerializablePool<ResearchPaginationDot> { }

        [Header("VFX")]
        public VfxPool ExplosionEffectPool;
        public VfxPool BoltZapEffectPool;

        // Material applied to the slot's rig renderer for the brief flash
        // before the burst. Optional — if null, the routine skips the swap.
        public Material PreExplodeItemMaterial;

        [Header("UI")]
        public DotPool PaginationDotPool;

        // Currently-allocated pagination dots, grown/shrunk by
        // HypothesisPanelVisualUtility against PaginationDotPool. List
        // tracks the active set so the visual util can iterate to apply
        // the per-dot confirmed-overlay state each frame and position the
        // shared CurrentHypothesisIndicator over the active dot.
        // Multiple hypothesis panels would need their own tracking;
        // today there is one in scope.
        [NonSerialized] public List<ResearchPaginationDot> ActivePaginationDots;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ExplosionEffectPool.Prewarm();
            BoltZapEffectPool.Prewarm();
            PaginationDotPool.Prewarm();
            ActivePaginationDots = new List<ResearchPaginationDot>(4);
            return null;
        }
    }
}

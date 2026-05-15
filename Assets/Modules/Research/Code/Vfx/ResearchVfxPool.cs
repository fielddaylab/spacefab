using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Holds the prewarmable VFX pools the Research minigame draws from for
    /// transient effects. Currently used by the explosion sequence (bolt-zap
    /// flash for VoltageBreakdown, then the main explosion burst). Pure data
    /// container plus an IScenePreload hook that prewarms the pools so the
    /// first explosion in a session does not incur instantiation cost.
    /// </summary>
    public class ResearchVfxPool : SharedStateComponent, IScenePreload {
        [Serializable] public sealed class VfxPool : SerializablePool<ResearchVfxInstance> { }

        public VfxPool ExplosionEffectPool;
        public VfxPool BoltZapEffectPool;

        // Material applied to the slot's rig renderer for the brief flash
        // before the burst. Optional — if null, the routine skips the swap.
        public Material PreExplodeItemMaterial;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ExplosionEffectPool.Prewarm();
            BoltZapEffectPool.Prewarm();
            return null;
        }
    }
}

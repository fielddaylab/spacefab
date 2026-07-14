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
        [Serializable] public sealed class ObservationChipPool : SerializablePool<ResearchObservationChip> { }

        [Header("VFX")]
        public VfxPool ExplosionEffectPool;
        public VfxPool BoltZapEffectPool;

        // Material applied to the slot's rig renderer for the brief flash
        // before the burst. Optional — if null, the routine skips the swap.
        public Material PreExplodeItemMaterial;

        [Header("UI")]
        public ObservationChipPool PickerChipPool;

        // Currently-allocated observation picker chips, grown/shrunk
        // once per chamber load by ObservationPickerLoadUtility against
        // PickerChipPool. ObservationPickerRefreshSystem walks this list
        // every frame the viewmodel changes to apply per-chip disabled
        // state. Single sample panel today; multiple would need their
        // own tracking.
        [NonSerialized] public List<ResearchObservationChip> ActivePickerChips;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ExplosionEffectPool.Prewarm();
            BoltZapEffectPool.Prewarm();
            PickerChipPool.Prewarm();
            ActivePickerChips = new List<ResearchObservationChip>(8);
            return null;
        }
    }
}

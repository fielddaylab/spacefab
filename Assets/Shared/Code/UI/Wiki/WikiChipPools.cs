using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.Research;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Pool backing the characteristics chips on material wiki pages. Registered on the boot
    /// prefab, so one chip set is reused across every material page the player opens.
    ///
    /// The chip prefab is ResearchObservationChip — the same view the Research picker uses, with
    /// ObservationType.ConfirmedProperty selecting the sprite bucket.
    /// </summary>
    public class WikiChipPools : SharedStateComponent, IScenePreload {
        [Serializable] public sealed class CharacteristicChipPool : SerializablePool<ResearchObservationChip> { }

        [Header("UI")]
        public CharacteristicChipPool ChipPool;

        // Currently-allocated chips, resized by WikiCharacteristicsLoadUtility on each rebuild and
        // read back on the next one to know what to free.
        [NonSerialized] public List<ResearchObservationChip> ActiveCharacteristicChips;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ChipPool.Prewarm();
            ActiveCharacteristicChips = new List<ResearchObservationChip>(8);
            return null;
        }
    }
}

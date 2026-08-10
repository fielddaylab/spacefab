using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using SpaceFab.Materials;
using SpaceFab.Research;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Pool backing the chips on material, observation, and property wiki pages. Registered on the
    /// boot prefab, so one chip set is reused across every page the player opens. Only one page is
    /// bound at a time, so the three active lists are never populated simultaneously — each load
    /// utility frees the other kinds' chips before allocating its own.
    ///
    /// The chip prefab is ResearchObservationChip — the same view the Research picker uses.
    /// </summary>
    public class WikiChipPools : SharedStateComponent, IScenePreload {
        [Serializable] public sealed class CharacteristicChipPool : SerializablePool<ResearchObservationChip> { }

        [Header("UI")]
        public CharacteristicChipPool ChipPool;

        // Currently-allocated chips, resized by WikiCharacteristicsLoadUtility on each rebuild and
        // read back on the next one to know what to free.
        [NonSerialized] public List<ResearchObservationChip> ActiveCharacteristicChips;

        // Observation-page chips and their parallel bookkeeping, owned by
        // WikiObservationLoadUtility. Labels resolve a chip back to the observation it renders; a
        // null handler entry marks a chip that was allocated without a click (non-Research scene),
        // keeping the three lists index-aligned.
        [NonSerialized] public List<ResearchObservationChip> ActiveObservationChips;
        [NonSerialized] public List<MaterialPropertyLabel> ActiveObservationLabels;
        [NonSerialized] public List<Action> ActiveObservationClickHandlers;

        // Property-page decomposed-observation chips, owned by WikiPropertyLoadUtility. Same
        // null-handler convention as the observation lists.
        [NonSerialized] public List<ResearchObservationChip> ActivePropertyLeafChips;
        [NonSerialized] public List<Action> ActivePropertyLeafClickHandlers;

        // Click handler bound to the authored (non-pooled) property chip while a property page is
        // displayed. Null when no property page is bound or the scene has no Research state.
        [NonSerialized] public Action PropertyChipClickHandler;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ChipPool.Prewarm();
            ActiveCharacteristicChips = new List<ResearchObservationChip>(8);
            ActiveObservationChips = new List<ResearchObservationChip>(8);
            ActiveObservationLabels = new List<MaterialPropertyLabel>(8);
            ActiveObservationClickHandlers = new List<Action>(8);
            ActivePropertyLeafChips = new List<ResearchObservationChip>(8);
            ActivePropertyLeafClickHandlers = new List<Action>(8);
            return null;
        }
    }
}

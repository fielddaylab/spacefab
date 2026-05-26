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
    /// Shared pools the wiki module draws from for material-page
    /// characteristics chips. Account-scoped (registered on the boot
    /// prefab); the chip set is reused across every material wiki page
    /// the player opens.
    ///
    /// Pattern mirrors ResearchPools: nested SerializablePool subclass
    /// + Active list + Prewarm in IScenePreload. Chip prefab is
    /// ResearchObservationChip — same view component the Research
    /// picker uses, with ObservationType.ConfirmedProperty driving the
    /// sprite bucket on the characteristics view.
    ///
    /// Follow-up: WikiPools (the existing BatchedComponent for tab /
    /// thumb buttons) still uses a manual Active/Free split. Migrate
    /// it to the SerializablePool pattern when convenient so the wiki
    /// module has a single pool style.
    /// </summary>
    public class WikiChipPools : SharedStateComponent, IScenePreload {
        [Serializable] public sealed class CharacteristicChipPool : SerializablePool<ResearchObservationChip> { }

        [Header("UI")]
        public CharacteristicChipPool ChipPool;

        // Currently-allocated characteristic chips, grown/shrunk on
        // wiki page-open (and on Research PropertyConfirmedThisFrame
        // while the Research minigame is loaded) by
        // WikiCharacteristicsLoadUtility against CharacteristicChipPool.
        // WikiCharacteristicsRefreshSystem iterates this to know what
        // to free before re-loading.
        [NonSerialized] public List<ResearchObservationChip> ActiveCharacteristicChips;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            ChipPool.Prewarm();
            ActiveCharacteristicChips = new List<ResearchObservationChip>(8);
            return null;
        }
    }
}

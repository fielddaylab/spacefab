using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using SpaceFab.Design;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpaceFab.Narrative {
    [CreateAssetMenu(menuName = "SpaceFab/Narrative/Character Asset")]
    public class CharacterDef : NamedAsset {
        public string DisplayName;
        public Color32 DialogueTint;

        [Header("Portraits")]
        // Portrait shown when no minigame scene is active (overarching, title, credits, etc.).
        // Inherits the value of the legacy "Portrait" field on existing assets via
        // FormerlySerializedAs, so PAL/Player/Dorian keep working without manual rewiring.
        [FormerlySerializedAs("Portrait")] public Sprite NonMinigamePortrait;

        // Portrait shown while a minigame scene is loaded. Leave null to fall back to
        // NonMinigamePortrait; ResolvePortrait handles the fallback in both directions.
        public Sprite MinigamePortrait;

        [AudioEvent] public StringHash32 CharacterTypeEvent;
        [AudioEvent] public StringHash32 DefaultQuip;

        /// <summary>
        /// Picks the right portrait for the current scene context. In a minigame, prefers
        /// MinigamePortrait and falls back to NonMinigamePortrait. Otherwise prefers
        /// NonMinigamePortrait and falls back to MinigamePortrait. Returns null only when
        /// both fields are unset.
        /// </summary>
        public static Sprite ResolvePortrait(CharacterDef def) {
            if (def == null) {
                return null;
            }

            // MinigameStateInterfacer lives on the Minigame Common prefab which is only
            // instantiated inside minigame scenes — its presence is the in-minigame signal.
            bool inMinigame = Game.SharedState.Has<MinigameStateInterfacer>();

            Sprite preferred = inMinigame ? def.MinigamePortrait : def.NonMinigamePortrait;
            if (preferred != null) { return preferred; }
            return inMinigame ? def.NonMinigamePortrait : def.MinigamePortrait;
        }
    }
}
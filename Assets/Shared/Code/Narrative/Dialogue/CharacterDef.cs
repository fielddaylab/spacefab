using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using SpaceFab.Design;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Narrative {
    [CreateAssetMenu(menuName = "SpaceFab/Character Asset")]
    public class CharacterDef : NamedAsset {
        public string DisplayName;
        public Color32 DialogueTint;
        public Sprite Portrait;
        [AudioEvent] public StringHash32 CharacterTypeEvent;
    }
}
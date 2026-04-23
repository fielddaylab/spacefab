using BeauRoutine;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.UI;
using SpaceFab.UI;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Narrative {
    public sealed class DialogueBox : BaseDialoguePrinter {
        #region Inspector

        [Header("Character")]
        [SerializeField] private TMP_Text m_CharacterName;
        [SerializeField] private Image m_CharacterPortrait;
        [SerializeField] private Graphic[] m_CharacterThemeTint;

        [Header("Text")]
        [SerializeField] private TMP_Text m_Contents;

        [Header("Next")]
        [SerializeField] private AutoSizedButton m_NextButton;

        #endregion // Inspector

        [NonSerialized] private CharacterDef m_CurrentCharacter;
        [NonSerialized] private DialogueCharacterState m_CurrentCharacterState;

        public override IEnumerator CompleteLine() {
            yield break;
        }

        public override void FastForwardLine(int visibleCount, int richCount) {
            m_Contents.maxVisibleCharacters += visibleCount;
        }

        public override IEnumerator TypeLine(TagString text, TagTextData textData, DialogueCharacterState character) {
            // TODO: animate to visible

            float dt = Routine.DeltaTime;
            float delay = -dt;
            bool bPlayType = false;

            int charsToShow = textData.VisibleCharacterCount;
            while(charsToShow > 0) {
                while(delay > 0) {
                    if (bPlayType) {
                        bPlayType = false;
                        PlayTypingSound();
                    }
                    yield return null;
                    dt = Routine.DeltaTime;
                    delay -= dt;
                }

                m_Contents.maxVisibleCharacters++;

                char displayed = text.VisibleText[m_Contents.maxVisibleCharacters - 1];
                switch(displayed) {

                }
            }
        }

        private float GetTypeDelay(float baseVal) {
            // TODO: Implement
            return baseVal;
        }

        private void PlayTypingSound() {

        }

        private void UpdateDecorationColors(CharacterDef def) {
            Color tint;
            if (def) {
                tint = def.DialogueTint;
            } else {
                tint = Color.white;
            }

            foreach (var tintable in m_CharacterThemeTint) {
                tintable.SetColor(tint);
            }
        }

        public override void UpdateCharacter(DialogueCharacterState character) {
            bool charChanged = false;
            CharacterDef def = m_CurrentCharacter;
            if (character.CharacterId != m_CurrentCharacterState.CharacterId) {
                m_CurrentCharacter = def = character.CharacterId.IsEmpty ? null : Find.NamedAsset<CharacterDef>(character.CharacterId);
                UpdateDecorationColors(def);
                charChanged = true;
            }

            if (charChanged || !string.Equals(character.OverrideName, m_CurrentCharacterState.OverrideName, StringComparison.Ordinal)) {
                string charName;
                if (!string.IsNullOrEmpty(character.OverrideName)) {
                    charName = character.OverrideName;
                } else if (def) {
                    charName = def.DisplayName;
                } else {
                    charName = "[NULL CHARACTER]";
                }

                m_CharacterName.SetText(charName);
            }

            if (charChanged || character.PoseId != m_CurrentCharacterState.PoseId) {
                Sprite portrait = null;
                if (def) {
                    portrait = def.Portrait;
                    // TODO: Implement per-pose portraits
                }
                m_CharacterPortrait.sprite = portrait;
            }

            m_CurrentCharacterState = character;
        }

        protected override void PrepareTextDisplay(TagString text, DialogueCharacterState character) {
            UpdateCharacter(character);

            m_Contents.SetText(text.RichText);
            m_Contents.maxVisibleCharacters = 0;
        }
    }
}
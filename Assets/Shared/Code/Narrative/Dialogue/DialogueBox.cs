using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scripting;
using FieldDay.UI;
using FieldDay.UI.Animation;
using Leaf.Runtime;
using SpaceFab.UI;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Narrative {
    public sealed class DialogueBox : BaseDialoguePrinter, ITypewriterModule {
        #region Inspector

        [Header("Character")]
        [SerializeField] private TMP_Text m_CharacterName;
        [SerializeField] private Image m_CharacterPortrait;
        [SerializeField] private Graphic[] m_CharacterThemeTint;

        [Header("Text")]
        [SerializeField] private TMP_Text m_Contents;

        [Header("Next")]
        [SerializeField] private AutoSizedButton m_NextButton;

        [Header("Animation")]
        [SerializeField] private CanvasGroup m_VisiblityGroup;
        [SerializeField] private LayoutOffset m_LayoutOffset;
        [SerializeField] private RectTransform m_FoldOutTransform;
        [SerializeField] private float m_FoldOutYPosDefault;
        [SerializeField] private float m_FoldOutYPosOffscreen;
        [SerializeField] private float m_FoldOutWidthDefault;
        [SerializeField] private float m_FoldOutWidthCollapsed;
        [SerializeField] private Graphic[] m_FoldOutCollapsedToHide;

        #endregion // Inspector

        [Flags]
        private enum LineFlags {
            AutoContinue = 0x01,
            IsEnd = 0x02
        }

        [NonSerialized] private IInputLayer m_InputLayer;

        [NonSerialized] private CharacterDef m_CurrentCharacter;
        [NonSerialized] private DialogueCharacterState m_CurrentCharacterState;
        [NonSerialized] private LineFlags m_CurrentLineFlags;

        [NonSerialized] private TypewriterEnumerator m_Typewriter = new TypewriterEnumerator();
        [NonSerialized] private long m_NextAllowedTypingSfx;
        [NonSerialized] private Routine m_Animation;
        [NonSerialized] private bool m_IsVisible;
        [NonSerialized] private AudioHandle m_CurrentQuip;

        private void Start() {
            m_VisiblityGroup.gameObject.SetActive(false);
            m_InputLayer = IInputLayer.Find(this);
        }

        public override IEnumerator CompleteLine() {
            if ((m_CurrentLineFlags & LineFlags.AutoContinue) != 0) {
                yield return 0.2f;
                yield break;
            }

            if (m_NextButton != null)
            {
                m_NextButton.gameObject.SetActive(true);
                if ((m_CurrentLineFlags & LineFlags.IsEnd) != 0)
                {
                    m_NextButton.TextContent.SetText("END CALL");
                }
                else
                {
                    m_NextButton.TextContent.SetText("NEXT");
                }

                m_NextButton.Layout.Sync();

                m_NextButton.ConsumeClick();
                while (!m_NextButton.ConsumeClick())
                {
                    yield return null;
                }
                m_NextButton.gameObject.SetActive(false);
            }
            else
            {
                while (true)
                {
                    // text box without next button remains up indefinitely until dismissed
                    yield return null;
                }
            }

            if ((m_CurrentLineFlags & LineFlags.IsEnd) != 0) {
                CurrentThread.ReleaseCurrentPrinter(ScriptThreadOwnershipClearReason.Completed);
            }
        }

        protected override void ConfigureEventHandler(TagStringEventHandler handler) {
            handler.Register(TagEvents.AutoContinue, () => m_CurrentLineFlags |= LineFlags.AutoContinue)
                .Register(TagEvents.InterpretAsClose, () => m_CurrentLineFlags |= LineFlags.IsEnd)
                .Register(TagEvents.PlayQuip, PlayQuipSfx);
        }

        public override void FastForwardLine(int visibleCount, int richCount) {
            m_Contents.maxVisibleCharacters += visibleCount;
        }

        public override IEnumerator TypeLine(TagString text, TagTextData textData, DialogueCharacterState character) {
            if (!m_IsVisible) {
                m_Animation.Stop();
                yield return AnimateToOn();
            } else if (m_Contents.maxVisibleCharacters == 0) {
                PopAnim.Play(m_LayoutOffset, PopAnim.Default);
            }

            m_Typewriter.Prepare(m_Contents, this, text, textData);
            yield return m_Typewriter;
        }

        private void PlayTypingSound() {
            long ts = Frame.Timestamp();
            if (ts >= m_NextAllowedTypingSfx) {
                if (m_CurrentCharacter && !m_CurrentCharacter.CharacterTypeEvent.IsEmpty) {
                    Sfx.Play(m_CurrentCharacter.CharacterTypeEvent);
                    m_NextAllowedTypingSfx = Frame.AdjustTimestamp(ts, 0.05f);
                }
            }
        }

        private void PlayQuipSfx(TagEventData evtData, object context) {
            StringHash32 art = evtData.Argument0.AsStringHash();
            if (m_CurrentCharacter) {
                StringHash32 quipEvent = m_CurrentCharacter.DefaultQuip;
                // TODO: find quip
                if (!quipEvent.IsEmpty) {
                    Sfx.Stop(m_CurrentQuip);
                    m_CurrentQuip = Sfx.Play(quipEvent);
                }
            }
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
            bool charChanged = !m_IsVisible;

            CharacterDef def = m_CurrentCharacter;
            if (charChanged || character.CharacterId != m_CurrentCharacterState.CharacterId) {
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
                // TODO: Implement per-pose portraits
                m_CharacterPortrait.sprite = CharacterDef.ResolvePortrait(def);
            }

            m_CurrentCharacterState = character;
        }

        protected override void PrepareTextDisplay(TagString text, DialogueCharacterState character) {
            UpdateCharacter(character);

            m_Contents.SetText(text.RichText);
            m_Contents.maxVisibleCharacters = 0;

            LineFlags flags = 0;
            if (LeafRuntime.PredictEnd(CurrentThread)) {
                flags |= LineFlags.IsEnd;
            }
            m_CurrentLineFlags = flags; 
        }

        protected override void OnThreadAcquired() {
            base.OnThreadAcquired();
        }

        protected override void OnThreadReleased() {
            if (m_IsVisible) {
                m_IsVisible = false;
                m_Animation.Replace(this, AnimateToOff());
            }
        }

        #region Typewriter

        void ITypewriterModule.GetTypewriterParameters(out float delayMultiplier, out TypewriterTimingTable timingTable) {
            if (CurrentThread.IsSkipping()) {
                delayMultiplier = -1;
                timingTable = default;
            } else {
                if (m_InputLayer.IsInputEnabled() && Game.Input.IsMouseDown(0)) {
                    delayMultiplier = 0.1f;
                } else {
                    delayMultiplier = 0.6f;
                }
                timingTable = DialoguePrinting.DefaultTimingTable;
            }
        }

        void ITypewriterModule.OnTypewriterType(char charValue, DialogueCharacterClass charClass) {
            if (charClass == DialogueCharacterClass.LetterOrDigit) {
                PlayTypingSound();
            }
        }

        #endregion // Typewriter

        #region Animations

        private IEnumerator AnimateToOn() {
            // TODO: Handle partially offscreen
            m_IsVisible = true;

            m_VisiblityGroup.blocksRaycasts = false;

            foreach (var foldoutObj in m_FoldOutCollapsedToHide) {
                foldoutObj.enabled = false;
            }

            m_NextButton?.gameObject.SetActive(false);

            m_FoldOutTransform.anchoredPosition = new Vector2(m_FoldOutTransform.anchoredPosition.x, m_FoldOutYPosOffscreen);
            Positioning.SetWidthDelta(m_FoldOutTransform, m_FoldOutWidthCollapsed);

            m_VisiblityGroup.gameObject.SetActive(true);
            Game.Gui.PushPriority(m_InputLayer);
            yield return null;

            yield return m_FoldOutTransform.AnchorPosTo(m_FoldOutYPosDefault, 0.2f, Axis.Y).Ease(Curve.BackOut);

            foreach(var obj in m_FoldOutCollapsedToHide) {
                obj.SetAlpha(0);
                obj.enabled = true;
            }
            
            yield return Routine.Combine(
                m_FoldOutTransform.SizeDeltaTo(m_FoldOutWidthDefault, 0.3f, Axis.X).Ease(Curve.CubeOut),
                Tween.ZeroToOne(SetFoldoutCollapsableObjectsAlpha, 0.2f).DelayBy(0.1f)
            );

            m_VisiblityGroup.blocksRaycasts = true;
        }

        private IEnumerator AnimateToOff() {
            m_IsVisible = false;

            m_VisiblityGroup.blocksRaycasts = false;
            Game.Gui.PopPriority(m_InputLayer);

            yield return m_FoldOutTransform.AnchorPosTo(m_FoldOutYPosOffscreen, 0.2f, Axis.Y).Ease(Curve.BackIn);

            m_VisiblityGroup.gameObject.SetActive(false);
        }

        private void SetFoldoutCollapsableObjectsAlpha(float alpha) {
            foreach(var obj in m_FoldOutCollapsedToHide) {
                obj.SetAlpha(alpha);
            }
        }

        #endregion // Animations
    }
}
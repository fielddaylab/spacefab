
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using System;
using System.Collections;
using System.Text;
using TMPro;

namespace FieldDay.Scripting {
    static public class DialoguePrinting {
        /// <summary>
        /// Returns the classification of this character.
        /// </summary>
        static public DialogueCharacterClass GetCharacterClass(char character) {
            switch(character) {
                case ' ': {
                    return DialogueCharacterClass.Space;
                }
                case '!':
                case '?':
                case '.': {
                    return DialogueCharacterClass.Terminator;
                }

                case ',': {
                    return DialogueCharacterClass.ClauseSeparator;
                }

                default: {
                    if (char.IsLetterOrDigit(character)) {
                        return DialogueCharacterClass.LetterOrDigit;
                    }

                    return DialogueCharacterClass.Other;
                }
            }
        }

        /// <summary>
        /// Default table.
        /// </summary>
        static public readonly TypewriterTimingTable DefaultTimingTable = new TypewriterTimingTable() {
            Space = 0.05f,
            ClauseSeparator = 0.08f,
            Terminator = 0.2f,
            LetterOrDigit = 0.03f,
            Other = 0.03f
        };
    }

    /// <summary>
    /// Typewriter callbacks.
    /// </summary>
    public interface ITypewriterModule {
        void OnCharacterTyped(char charValue, DialogueCharacterClass charClass);
    }
    
    /// <summary>
    /// Typewriter enumerator.
    /// </summary>
    public sealed class TypewriterEnumerator : IEnumerator {
        // Inputs
        public float Multiplier = 1;
        public bool Skip = false;

        // Components
        private ITypewriterModule m_Module;
        private TMP_Text m_Text;
        private StringBuilder m_Data;
        private TypewriterTimingTable m_Timing;

        // State
        private float m_Delay;
        private int m_CharsToType;

        #region Configuration

        public void ConfigureComponents(TMP_Text text, ITypewriterModule module) {
            m_Text = text;
            m_Module = module;
        }

        public void ConfigureTiming(in TypewriterTimingTable timingTable) {
            m_Timing = timingTable;
        }

        #endregion // Configuration

        public void Prepare(TagString data, TagTextData textData) {
            Prepare(data.VisibleText, textData.VisibleCharacterCount);
        }

        public void Prepare(StringBuilder data, int visibleCharCount) {
            m_Data = data;
            m_CharsToType = visibleCharCount;
            m_Delay = -Routine.DeltaTime;
        }

        object IEnumerator.Current {
            get { return null; }
        }

        public bool MoveNext() {
            if (Skip) {
                return false;
            }

            if (m_Delay > 0) {
                m_Delay -= Routine.DeltaTime;
                return true;
            }

            while(m_CharsToType-- > 0 && m_Delay <= 0) {
                int charIndex = m_Text.maxVisibleCharacters++;
                char value = m_Data[charIndex];
                DialogueCharacterClass charClass = DialoguePrinting.GetCharacterClass(value);
                m_Delay += Multiplier * m_Timing[charClass];
                m_Module.OnCharacterTyped(value, charClass);
            }

            return m_CharsToType > 0;
        }

        void IEnumerator.Reset() {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Timing table.
    /// </summary>
    [Serializable]
    public struct TypewriterTimingTable {
        public float Space;
        public float ClauseSeparator;
        public float Terminator;
        public float LetterOrDigit;
        public float Other;

        public unsafe float this[DialogueCharacterClass index] {
            get {
                fixed(float* ptr = &Space) {
                    return ptr[(int)index];
                }
            }
        }
    }

    public enum DialogueCharacterClass : byte {
        Space,
        ClauseSeparator,
        Terminator,
        LetterOrDigit,
        Other
    }
}
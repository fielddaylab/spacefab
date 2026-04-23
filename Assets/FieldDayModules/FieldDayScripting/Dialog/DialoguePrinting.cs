
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
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
        void GetTypewriterParameters(out float delayMultiplier, out TypewriterTimingTable timingTable);
        void OnTypewriterType(char charValue, DialogueCharacterClass charClass);
    }
    
    /// <summary>
    /// Typewriter enumerator.
    /// </summary>
    public sealed class TypewriterEnumerator : IEnumerator, IDisposable {
        private enum State : uint {
            Initialize,
            Running,
            Done,
        }

        // Components
        private ITypewriterModule m_Module;
        private TMP_Text m_Text;
        private StringBuilder m_Data;

        // State
        private float m_Delay;
        private int m_CharsToType;
        private State m_State;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Prepare(TMP_Text text, ITypewriterModule module, TagString data, TagTextData textData) {
            Prepare(text, module, data.VisibleText, textData.VisibleCharacterCount);
        }

        public void Prepare(TMP_Text text, ITypewriterModule module, StringBuilder data, int visibleCharCount) {
            Assert.NotNullOrDestroyed(text);
            Assert.NotNull(module);

            m_Text = text;
            m_Module = module;
            m_Data = data;
            m_CharsToType = visibleCharCount;
            m_State = State.Initialize;
        }

        #region IEnumerator

        object IEnumerator.Current {
            get { return null; }
        }

        public bool MoveNext() {
            switch(m_State) {
                case State.Done: {
                    return false;
                }

                case State.Initialize: {
                    m_Delay = -Routine.DeltaTime;
                    m_State = State.Running;
                    break;
                }
            }

            float multiplier = 1;
            TypewriterTimingTable timingTable = DialoguePrinting.DefaultTimingTable;
            m_Module?.GetTypewriterParameters(out multiplier, out timingTable);

            if (multiplier <= 0) {
                m_Text.maxVisibleCharacters += m_CharsToType;
                m_State = State.Done;
                return false;
            }

            if (m_Delay > 0) {
                m_Delay -= Routine.DeltaTime;
                return true;
            }

            while(m_Delay <= 0 && m_CharsToType-- > 0) {
                int charIndex = m_Text.maxVisibleCharacters++;
                char value = m_Data[charIndex];
                DialogueCharacterClass charClass = DialoguePrinting.GetCharacterClass(value);
                m_Delay += multiplier * timingTable[charClass];
                m_Module?.OnTypewriterType(value, charClass);
            }

            if (m_CharsToType <= 0) {
                m_State = State.Done;
                return false;
            }

            return true;
        }

        void IEnumerator.Reset() {
            throw new NotSupportedException();
        }

        #endregion // IEnumerator

        #region IDisposable

        void IDisposable.Dispose() {
            m_Delay = 0;
            m_State = State.Initialize;
            m_CharsToType = 0;
            m_Data = null;
            m_Text = null;
            m_Module = null;
        }

        #endregion // IDisposable
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
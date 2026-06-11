using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using FieldDay.Components;
using Leaf.Runtime;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.Scripting {
    public abstract class BaseDialoguePrinter : BatchedComponent, IRegistrationCallbacks, IDialoguePrinter {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id;

        #endregion // Inspector

        private ScriptThread m_OwnerThread;
        private TagStringEventHandler m_OverrideHandler;

        #region IRegistrationCallbacks

        public virtual void OnRegister() {
            ScriptUtility.RegisterDialoguePrinter(m_Id, this);
        }
        
        public virtual void OnDeregister() {
            ScriptUtility.DeregisterDialoguePrinter(m_Id, this);
        }

        #endregion // IRegistrationCallbacks

        #region IScriptThreadOwned

        public LeafThreadHandle ThreadOwner { get; set; }

        void IScriptThreadOwned.OnThreadRelease(LeafThreadHandle threadHandle, ScriptThreadOwnershipClearReason cancelType) {
            if (cancelType == ScriptThreadOwnershipClearReason.Switch) {
                threadHandle.Kill();
            }
            m_OverrideHandler.Base = null;
            m_OwnerThread = null;
            OnThreadReleased();
        }

        void IScriptThreadOwned.OnThreadAcquire(LeafThreadHandle threadHandle) {
            m_OwnerThread = threadHandle.GetThread<ScriptThread>();
            OnThreadAcquired();
        }

        #endregion // IScriptThreadOwned

        public virtual void StartSkip() { }
        public virtual void CancelSkip() { }

        public TagStringEventHandler PrepareLine(TagString text, DialogueCharacterState character, TagStringEventHandler parentHandler) {
            if (text.RichText.Length == 0) {
                return null;
            }

            if (m_OverrideHandler == null) {
                m_OverrideHandler = new TagStringEventHandler();
                ConfigureEventHandler(m_OverrideHandler);
            }
            m_OverrideHandler.Base = parentHandler;

            PrepareTextDisplay(text, character);
            return m_OverrideHandler;
        }

        protected virtual void ConfigureEventHandler(TagStringEventHandler handler) { }
        protected abstract void PrepareTextDisplay(TagString text, DialogueCharacterState character);

        public abstract IEnumerator TypeLine(TagString text, TagTextData textData, DialogueCharacterState character);
        public abstract void FastForwardLine(int visibleCount, int richCount);
        public abstract void UpdateCharacter(DialogueCharacterState character);
        public abstract IEnumerator CompleteLine();

        protected virtual void OnThreadAcquired() { }

        protected virtual void OnThreadReleased() { }

        protected ScriptThread CurrentThread {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_OwnerThread; }
        }
    }
}
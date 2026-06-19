using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay.Audio;
using System;
using UnityEngine;

namespace FieldDay.UI {
    [RequireComponent(typeof(PointerListener))]
    public sealed class GuiPointerSounds : MonoBehaviour {
        [Header("Events")]
        [AudioEvent] public StringHash32 OnEnter;
        [AudioEvent] public StringHash32 OnExit;
        [AudioEvent] public StringHash32 OnDown;
        [AudioEvent] public StringHash32 OnUp;
        [AudioEvent] public StringHash32 OnClick;

        [NonSerialized] private Transform m_CachedTransform;

        private unsafe void Start() {
            this.CacheComponent(ref m_CachedTransform);

            PointerListener listener = GetComponent<PointerListener>();
            Assert.NotNullOrDestroyed(listener, "No PointerListener present!");

#if UNITY_EDITOR
            listener.onPointerEnter.Register(OnPointerEnter);
            listener.onPointerExit.Register(OnPointerExit);
            listener.onPointerDown.Register(OnPointerDown);
            listener.onPointerUp.Register(OnPointerUp);
            listener.onClick.Register(OnPointerClicked);
#else
            listener.onPointerEnter.Register(&OnPointerEnter);
            listener.onPointerExit.Register(&OnPointerExit);
            listener.onPointerDown.Register(&OnPointerDown);
            listener.onPointerUp.Register(&OnPointerUp);
            listener.onClick.Register(&OnPointerClicked);
#endif // UNITY_EDITOR
        }

        static private void PlaySfx(StringHash32 sound, Transform position) {
            if (!sound.IsEmpty) {
                Sfx.PlayDetached(sound, position);
            }
        }

        static private void OnPointerEnter(PointerListener.EventData evtData) {
            if (evtData.Source.TryGetComponent(out GuiPointerSounds sounds)) {
                PlaySfx(sounds.OnEnter, sounds.m_CachedTransform);
            }
        }

        static private void OnPointerExit(PointerListener.EventData evtData) {
            if (evtData.Source.TryGetComponent(out GuiPointerSounds sounds)) {
                PlaySfx(sounds.OnExit, sounds.m_CachedTransform);
            }
        }

        static private void OnPointerDown(PointerListener.EventData evtData) {
            if (evtData.Source.TryGetComponent(out GuiPointerSounds sounds)) {
                PlaySfx(sounds.OnDown, sounds.m_CachedTransform);
            }
        }

        static private void OnPointerUp(PointerListener.EventData evtData) {
            if (evtData.Source.TryGetComponent(out GuiPointerSounds sounds)) {
                PlaySfx(sounds.OnUp, sounds.m_CachedTransform);
            }
        }

        static private void OnPointerClicked(PointerListener.EventData evtData) {
            if (evtData.Source.TryGetComponent(out GuiPointerSounds sounds)) {
                PlaySfx(sounds.OnClick, sounds.m_CachedTransform);
            }
        }
    }
}
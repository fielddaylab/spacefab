using System;
using System.Collections;
using BeauRoutine;
using SpaceFab.UI;
using UnityEngine;

namespace SpaceFab.Narrative {
    /// <summary>
    /// In-minigame dialogue printer. Differs from DialogueBox in three ways:
    ///   - CompleteLine returns immediately (no Next-button gate), so script threads advance
    ///     line-to-line without waiting for player input.
    ///   - Stays visible after the script thread releases — dismissal is explicit, via the
    ///     close button or the LeafMember surface in DialogueScripting.
    ///   - Does not push input priority. Other minigame UI stays interactable while dialogue
    ///     is on screen.
    /// </summary>
    public sealed class MinigameDialogueBox : BaseSpacefabDialogueBox {
        #region Inspector

        [Header("Animation")]
        [SerializeField] private CanvasGroup m_VisiblityGroup;
        [SerializeField] private float m_FadeDuration = 0.2f;

        [Header("Close")]
        // Optional. When assigned, clicking dismisses the box. Leave null for dialogue that
        // can only be dismissed via the Leaf LeafMember API.
        [SerializeField] private DynamicButton m_CloseButton;

        #endregion // Inspector

        private void Start() {
            m_VisiblityGroup.gameObject.SetActive(false);
            m_VisiblityGroup.alpha = 0f;
            m_VisiblityGroup.blocksRaycasts = false;

            if (m_CloseButton != null) {
                m_CloseButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        protected override void OnDisable() {
            // No input-priority cleanup needed — the minigame box never pushes priority. But
            // make sure any in-flight fade is stopped so the routine doesn't continue against
            // a disabled GameObject.
            m_Animation.Stop();
            base.OnDisable();
        }

        private void OnDestroy() {
            if (m_CloseButton != null) {
                m_CloseButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }

        public override IEnumerator CompleteLine() {
            // No Next-button gate in the minigame variant — line completion is purely a function
            // of typewriter completion, which has already happened by the time TypeLine returned.
            // Yield break lets the script thread immediately advance to whatever comes next.
            yield break;
        }

        protected override void OnThreadReleased() {
            // Stay visible after the script ends. The box hangs around until Dismiss() is
            // called explicitly (close button or LeafMember). Overarching DialogueBox's
            // auto-hide-on-release behaviour is the wrong default here.
        }

        /// <summary>
        /// Explicitly dismisses the box: fades out and deactivates. Safe to call when already
        /// hidden (no-op). Invoked by the close-button listener and by the Leaf-callable
        /// dismiss entry point in DialogueScripting.
        /// </summary>
        public void Dismiss() {
            if (!m_IsVisible) { return; }
            m_Animation.Replace(this, AnimateToOff());
        }

        private void HandleCloseClicked() {
            Dismiss();
        }

        #region Animations

        protected override IEnumerator AnimateToOn() {
            m_IsVisible = true;
            m_VisiblityGroup.gameObject.SetActive(true);
            m_VisiblityGroup.blocksRaycasts = true;
            yield return m_VisiblityGroup.FadeTo(1f, m_FadeDuration);
        }

        protected override IEnumerator AnimateToOff() {
            m_IsVisible = false;
            m_VisiblityGroup.blocksRaycasts = false;
            yield return m_VisiblityGroup.FadeTo(0f, m_FadeDuration);
            m_VisiblityGroup.gameObject.SetActive(false);
        }

        #endregion // Animations
    }
}

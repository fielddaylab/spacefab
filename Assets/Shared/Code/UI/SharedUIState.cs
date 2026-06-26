using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public sealed class SharedUIState : SharedStateComponent, IRegistrationCallbacks
    {
        public LoadIcon LoadIcon;
        public SaveIcon SaveIcon;
        public CursorHint LoadingCursor;

        // The save and load flows can overlap (a minigame-exit save runs concurrently with the
        // return-scene load) and both drive the same exclusively-owned LoadingCursor hint and the
        // global Game.Input raycast pause. Treat them as a shared resource owned for as long as
        // EITHER flow is active: acquire (lock cursor + pause raycasts) when the first flow starts,
        // release when the last flow finishes. IsLoading / isSaving together are the active-flow
        // set; see OnBeginLoading / OnBeginSave (acquire) and OnLoadingComplete / OnSaveSuccess /
        // OnSaveError (release).
        public bool IsLoading;
        public bool isSaving;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            SharedUIUtility.Init(this);
        }
    }

    public static class SharedUIUtility
    {
        #region Init

        public static void Init(SharedUIState ui)
        {
            SetLoadIconVisible(ui.LoadIcon, false);
            SetSaveIconVisible(ui.SaveIcon, false);
        }

        #endregion // Init

        #region General

        private static void SetLoadIconVisible(LoadIcon icon, bool isVisible)
        {
            icon.Group.alpha = isVisible ? 1 : 0;
        }

        public static IEnumerator FadeInLoadIcon(SharedUIState ui, float inTime)
        {
            ui.LoadIcon.gameObject.SetActive(true);
            yield return ui.LoadIcon.Group.FadeTo(1, inTime);
        }

        public static IEnumerator FadeOutLoadIcon(SharedUIState ui, float inTime)
        {
            yield return ui.LoadIcon.Group.FadeTo(0, inTime);
            ui.LoadIcon.gameObject.SetActive(false);
        }

        private static void SetSaveIconVisible(SaveIcon icon, bool isVisible)
        {
            icon.Group.alpha = isVisible ? 1 : 0;
        }

        public static IEnumerator FadeInSaveIcon(SharedUIState ui, float inTime)
        {
            ui.SaveIcon.gameObject.SetActive(true);
            yield return ui.SaveIcon.Group.FadeTo(1, inTime);
        }

        public static IEnumerator FadeOutSaveIcon(SharedUIState ui, float inTime)
        {
            yield return ui.SaveIcon.Group.FadeTo(0, inTime);
            ui.SaveIcon.gameObject.SetActive(false);
        }

        #endregion General

        #region Transition Gate

        // True while either the save or the load flow is active. While held, the LoadingCursor is
        // locked and the global raycast pause is engaged.
        private static bool AnyTransitionActive(SharedUIState uiState)
        {
            return uiState.IsLoading || uiState.isSaving;
        }

        // Engages the shared transition resource (cursor lock + raycast pause) iff it isn't already
        // held by the other flow. Input enable/disable is NOT part of the shared gate — only the
        // load flow disables input (a standalone save must leave input responsive). Call AFTER
        // setting this flow's active flag so otherFlowActive reflects only the OTHER flow.
        private static void AcquireTransitionGate(SharedUIState uiState, bool otherFlowActive)
        {
            if (otherFlowActive) { return; }   // already engaged by the other flow

            Game.Input.PauseRaycasts();
            CursorHint.TryLock(uiState.LoadingCursor);
        }

        // Releases the shared transition resource iff no flow remains active. Call AFTER clearing
        // this flow's active flag so AnyTransitionActive reflects the post-clear state.
        private static void ReleaseTransitionGate(SharedUIState uiState)
        {
            if (AnyTransitionActive(uiState)) { return; }   // the other flow still holds it

            Game.Input.ResumeRaycasts();
            CursorHint.Unlock(uiState.LoadingCursor);
        }

        #endregion // Transition Gate

        #region Loading

        public static IEnumerator OnBeginLoading(SharedUIState uiState)
        {
            bool otherFlowActive = uiState.isSaving;
            if (!uiState.IsLoading) {
                uiState.IsLoading = true;
                AcquireTransitionGate(uiState, otherFlowActive);
                // Input disable is load-specific: block interaction across the scene swap.
                InputUtility.SetInputEnabled(Find.State<InputState>(), false);
            }

            // TODO: begin loading animation
            uiState.LoadIcon.LoadingText.SetText("Loading");
            yield return FadeInLoadIcon(uiState, 0.1f);
        }

        public static IEnumerator OnLoadingComplete(SharedUIState uiState)
        {
            uiState.LoadIcon.LoadingText.SetText("Loaded!");
            yield return FadeOutLoadIcon(uiState, 0.1f);

            // wait for save to complete
            while (uiState.isSaving)
            {
                yield return null;
            }

            uiState.IsLoading = false;
            // Re-enable input on the (post-reload) InputState. Always-sync in SetInputEnabled
            // guarantees the new scene's raycaster is turned back on even if the flag is unchanged.
            InputUtility.SetInputEnabled(Find.State<InputState>(), true);
            ReleaseTransitionGate(uiState);
        }

        #endregion // Loading

        #region Saving

        public static IEnumerator OnBeginSave(SharedUIState uiState)
        {
            bool otherFlowActive = uiState.IsLoading;
            if (!uiState.isSaving) {
                uiState.isSaving = true;
                AcquireTransitionGate(uiState, otherFlowActive);
            }

            //uiState.FaderGroup.blocksRaycasts = true;
            //uiState.FaderGroup.alpha = 1;

            // TODO: begin loading animation
            uiState.SaveIcon.SavingText.SetText("Saving...");
            yield return FadeInSaveIcon(uiState, 0.25f);
        }

        public static IEnumerator OnSaveSuccess(SharedUIState uiState)
        {
            // TODO: switch to loading complete animation
            yield return 0.5f;
            uiState.SaveIcon.SavingText.SetText("Saved!");
            yield return FadeOutSaveIcon(uiState, 0.5f);

            // disperse fader
            // yield return FadeOut(uiState, 1.5f);

            uiState.isSaving = false;
            ReleaseTransitionGate(uiState);
        }

        public static IEnumerator OnSaveError(SharedUIState uiState)
        {
            // TODO: switch to loading error animation
            yield return 0.5f;
            uiState.SaveIcon.SavingText.SetText("Save Failed!");
            yield return FadeOutSaveIcon(uiState, 0.5f);

            // disperse fader
            // yield return FadeOut(uiState, 1.5f);

            uiState.isSaving = false;
            ReleaseTransitionGate(uiState);
        }

        #endregion // Saving

    }
}

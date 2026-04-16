using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public class SharedUIState : SharedStateComponent, IRegistrationCallbacks
    {
        public CanvasGroup FaderGroup;
        public LoadIcon LoadIcon;
        public SaveIcon SaveIcon;
        public CursorHint LoadingCursor;

        public bool CursorWasLocked;
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
            ui.FaderGroup.blocksRaycasts = false;
            ui.FaderGroup.alpha = 0;
            SetLoadIconVisible(ui.LoadIcon, false);
            SetSaveIconVisible(ui.SaveIcon, false);
        }

        #endregion // Init

        #region General

        public static IEnumerator FadeIn(SharedUIState ui, float inTime)
        {
            ui.FaderGroup.blocksRaycasts = true;
            yield return ui.FaderGroup.FadeTo(1, inTime);
        }

        public static IEnumerator FadeOut(SharedUIState ui, float inTime)
        {
            yield return ui.FaderGroup.FadeTo(0, inTime);
            ui.FaderGroup.blocksRaycasts = false;
        }

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

        #region Loading

        public static IEnumerator OnBeginLoading(SharedUIState uiState)
        {
            uiState.IsLoading = true;
            uiState.CursorWasLocked = CursorHint.IsLocked(uiState.LoadingCursor);
            InputState input = Find.State<InputState>();
            Game.Input.PauseRaycasts();
            InputUtility.SetInputEnabled(input, false);
            CursorHint.TryLock(uiState.LoadingCursor);

            uiState.FaderGroup.blocksRaycasts = true;
            uiState.FaderGroup.alpha = 1;

            // TODO: begin loading animation
            uiState.LoadIcon.LoadingText.SetText("Loading");
            yield return FadeInLoadIcon(uiState, 0.25f);
        }

        public static IEnumerator OnLoadingComplete(SharedUIState uiState)
        {
            // TODO: switch to loading complete animation
            yield return 0.5f;
            uiState.LoadIcon.LoadingText.SetText("Loaded!");
            yield return FadeOutLoadIcon(uiState, 0.5f);

            // wait for save to complete
            while (uiState.isSaving)
            {
                yield return null;
            }

            // disperse fader
            yield return FadeOut(uiState, 1.5f);

            InputState input = Find.State<InputState>();
            Game.Input.ResumeRaycasts();
            InputUtility.SetInputEnabled(input, true);
            if (!uiState.CursorWasLocked)
            {
                CursorHint.Unlock(uiState.LoadingCursor);
            }
            uiState.CursorWasLocked = false;
            uiState.IsLoading = false;
        }

        #endregion // Loading

        #region Saving

        public static IEnumerator OnBeginSave(SharedUIState uiState)
        {
            uiState.CursorWasLocked = CursorHint.IsLocked(uiState.LoadingCursor);
            uiState.isSaving = true;
            Game.Input.PauseRaycasts();
            // InputUtility.SetInputEnabled(input, false);
            CursorHint.TryLock(uiState.LoadingCursor);

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

            InputState input = Find.State<InputState>();
            Game.Input.ResumeRaycasts();
            // InputUtility.SetInputEnabled(input, true);
            if (!uiState.CursorWasLocked)
            {
                CursorHint.Unlock(uiState.LoadingCursor);
            }
            uiState.isSaving = false;
            uiState.CursorWasLocked = false;
        }

        public static IEnumerator OnSaveError(SharedUIState uiState)
        {
            // TODO: switch to loading error animation
            yield return 0.5f;
            uiState.SaveIcon.SavingText.SetText("Save Failed!");
            yield return FadeOutSaveIcon(uiState, 0.5f);

            // disperse fader
            // yield return FadeOut(uiState, 1.5f);

            InputState input = Find.State<InputState>();
            Game.Input.ResumeRaycasts();
            InputUtility.SetInputEnabled(input, true);
            if (!uiState.CursorWasLocked)
            {
                CursorHint.Unlock(uiState.LoadingCursor);
            }
            uiState.isSaving = false;
            uiState.CursorWasLocked = false;
        }

        #endregion // Saving

    }
}

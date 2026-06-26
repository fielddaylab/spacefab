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

        #region Loading

        public static IEnumerator OnBeginLoading(SharedUIState uiState)
        {
            // TODO: begin loading animation
            uiState.LoadIcon.LoadingText.SetText("Loading");
            yield return FadeInLoadIcon(uiState, 0.1f);
        }

        public static IEnumerator OnLoadingComplete(SharedUIState uiState)
        {
            uiState.LoadIcon.LoadingText.SetText("Loaded!");
            yield return FadeOutLoadIcon(uiState, 0.1f);
        }

        #endregion // Loading

        #region Saving

        public static IEnumerator OnBeginSave(SharedUIState uiState)
        {
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
        }

        public static IEnumerator OnSaveError(SharedUIState uiState)
        {
            // TODO: switch to loading error animation
            yield return 0.5f;
            uiState.SaveIcon.SavingText.SetText("Save Failed!");
            yield return FadeOutSaveIcon(uiState, 0.5f);
        }

        #endregion // Saving

    }
}

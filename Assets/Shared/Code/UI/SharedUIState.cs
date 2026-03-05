using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacefab.Shared
{
    public class SharedUIState : SharedStateComponent, IRegistrationCallbacks
    {
        public CanvasGroup FaderGroup;
        public LoadIcon LoadIcon;

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
            ui.FaderGroup.alpha = 0;
            SetLoadIconVisible(ui.LoadIcon, false);
        }

        #endregion // Init

        #region General

        public static IEnumerator FadeIn(SharedUIState ui, float inTime)
        {
            yield return ui.FaderGroup.FadeTo(1, inTime);
        }

        public static IEnumerator FadeOut(SharedUIState ui, float inTime)
        {
            yield return ui.FaderGroup.FadeTo(0, inTime);
        }

        private static void SetLoadIconVisible(LoadIcon icon, bool isVisible)
        {
            icon.Group.alpha = isVisible ? 1 : 0;
        }

        public static IEnumerator FadeInIcon(SharedUIState ui, float inTime)
        {
            ui.LoadIcon.gameObject.SetActive(true);
            yield return ui.LoadIcon.Group.FadeTo(1, inTime);
        }

        public static IEnumerator FadeOutIcon(SharedUIState ui, float inTime)
        {
            yield return ui.LoadIcon.Group.FadeTo(0, inTime);
            ui.LoadIcon.gameObject.SetActive(false);
        }

        #endregion General

        #region Specific

        public static IEnumerator OnBeginLoading(SharedUIState uiState)
        {
            yield return FadeIn(uiState, 0);

            // TODO: begin loading animation
            yield return FadeInIcon(uiState, 0.25f);
        }

        public static IEnumerator OnLoadingComplete(SharedUIState uiState)
        {
            // TODO: switch to loading complete animation
            yield return 0.5f;
            yield return FadeOutIcon(uiState, 0.5f);

            // disperse fader
            yield return FadeOut(uiState, 1.5f);
        }

        #endregion // Specific
    }
}

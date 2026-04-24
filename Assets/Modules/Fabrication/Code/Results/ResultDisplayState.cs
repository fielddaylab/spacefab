using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public class ResultDisplayState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool DisplayRequestedThisFrame;

        public CanvasGroup ResultsGroup;

        public Routine ResultsTransitionRoutine;

        public void OnDeregister()
        {
            ResultsTransitionRoutine.Stop();
        }

        public void OnRegister()
        {
            ResultDisplayStateUtility.SetEnabledResultsGroup(this, false);
        }
    }

    public static class ResultDisplayStateUtility
    {
        public static void SetEnabledResultsGroup(ResultDisplayState displayState, bool isEnabled)
        {
            displayState.ResultsGroup.alpha = isEnabled ? 1 : 0;
            displayState.ResultsGroup.blocksRaycasts = isEnabled;
            displayState.ResultsGroup.interactable = isEnabled;
        }

        public static void ShowResults(ResultDisplayState displayState)
        {
            displayState.ResultsTransitionRoutine.Replace(ShowResultsRoutine(displayState));
        }

        public static void HideResults(ResultDisplayState displayState)
        {
            displayState.ResultsTransitionRoutine.Replace(HideResultsRoutine(displayState));
        }

        private static IEnumerator ShowResultsRoutine(ResultDisplayState displayState)
        {
            SetEnabledResultsGroup(displayState, true);
            yield break;
        }

        private static IEnumerator HideResultsRoutine(ResultDisplayState displayState)
        {
            SetEnabledResultsGroup(displayState, false);
            yield break;
        }
    }
}

using System.Collections;
using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.UI;
using TMPro;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Holds data relevant to displaying simulation results.
    /// </summary>
    public class ResultState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool DisplayRequestedThisFrame;
        [HideInInspector] public bool AllCorrect;

        public CanvasGroup ResultsGroup;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI SummaryText;
        public DynamicButton DismissButton;
        public DynamicButton RetryButton;

        [HideInInspector] public Routine ResultsTransitionRoutine;

        public void OnRegister()
        {
            ResultStateUtility.SetEnabledResultsGroup(this, false);

            if (DismissButton != null)
            {
                DismissButton.onClick.AddListener(() => ResultStateUtility.HideResults(this));
            }

            if (RetryButton != null)
            {
                RetryButton.onClick.AddListener(() => {
                    ResultStateUtility.HideResults(this);
                    // Hook into Simulate-mode rerun logic from the caller if needed.
                });
            }
        }

        public void OnDeregister()
        {
            ResultsTransitionRoutine.Stop();

            if (DismissButton != null)
            {
                DismissButton.onClick.RemoveAllListeners();
            }

            if (RetryButton != null)
            {
                RetryButton.onClick.RemoveAllListeners();
            }
        }
    }

    public static class ResultStateUtility
    {
        public static void SetEnabledResultsGroup(ResultState resultState, bool isEnabled)
        {
            if (resultState.ResultsGroup == null)
                return;

            resultState.ResultsGroup.alpha = isEnabled ? 1f : 0f;
            resultState.ResultsGroup.blocksRaycasts = isEnabled;
            resultState.ResultsGroup.interactable = isEnabled;
        }

        public static void ShowResults(ResultState resultState, bool allCorrect = false)
        {
            if (resultState.TitleText != null)
            {
                resultState.TitleText.SetText(allCorrect ? "Success" : "Review Results");
            }

            if (resultState.SummaryText != null)
            {
                resultState.SummaryText.SetText(allCorrect ? "All outputs matched the expected values." : "Some outputs were incorrect or unstable.");
            }

            resultState.ResultsTransitionRoutine.Replace(ShowResultsRoutine(resultState));
        }

        public static void HideResults(ResultState resultState)
        {
            resultState.ResultsTransitionRoutine.Replace(HideResultsRoutine(resultState));
        }

        private static IEnumerator ShowResultsRoutine(ResultState resultState)
        {
            SetEnabledResultsGroup(resultState, true);
            yield break;
        }

        private static IEnumerator HideResultsRoutine(ResultState resultState)
        {
            SetEnabledResultsGroup(resultState, false);
            yield break;
        }
    }
}

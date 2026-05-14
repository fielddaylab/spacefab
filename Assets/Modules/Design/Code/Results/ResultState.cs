using FieldDay;
using FieldDay.SharedState;
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
        public CanvasGroup ResultsGroup;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI SummaryText;
        public DynamicButton DismissButton;
        public DynamicButton RetryButton;

        public void OnRegister()
        {
            ResultStateUtility.SetEnabledResultsGroup(this, false);

            if (DismissButton != null)
            {
                DismissButton.gameObject.SetActive(true);
                DismissButton.onClick.AddListener(OnDismissClicked);
            }

            if (RetryButton != null)
            {
                RetryButton.gameObject.SetActive(true);
                RetryButton.onClick.AddListener(OnRetryClicked);
            }
        }

        public void OnDeregister()
        {
            if (DismissButton != null)
            {
                DismissButton.onClick.RemoveListener(OnDismissClicked);
            }

            if (RetryButton != null)
            {
                RetryButton.onClick.RemoveListener(OnRetryClicked);
            }
        }

        private void OnDismissClicked()
        {
            // Set the request flag so SimulateModeSystem.ProcessSuiteComplete transitions
            // to Idle on its next tick. Hide the panel immediately so it doesn't linger.
            Find.State(out SimulateRunState runState);
            runState.DismissResultsRequested = true;
            ResultStateUtility.SetEnabledResultsGroup(this, false);
        }

        private void OnRetryClicked()
        {
            // Hide panel immediately, then queue a full-suite rerun.
            // SimulateModeSystem.ProcessSuiteComplete picks up PlayFullSuiteRequested
            // on its next tick and transitions to PreparingTest.
            Find.State(out SimulateRunState runState);
            ResultStateUtility.SetEnabledResultsGroup(this, false);
            runState.PlayFullSuiteRequested = true;
        }
    }

    public static class ResultStateUtility
    {
        public static void SetEnabledResultsGroup(ResultState resultState, bool isEnabled)
        {
            if (resultState.ResultsGroup == null)
            {
                Debug.LogWarning("ResultState.ResultsGroup is null!");
                return;
            }

            resultState.ResultsGroup.alpha = isEnabled ? 1f : 0f;
            resultState.ResultsGroup.blocksRaycasts = isEnabled;
            resultState.ResultsGroup.interactable = isEnabled;
            Debug.Log($"SetEnabledResultsGroup: enabled={isEnabled}, alpha={resultState.ResultsGroup.alpha}");
        }
        public static void ShowResults(ResultState resultState, bool allCorrect)
        {
            Debug.Log($"ResultStateUtility.ShowResults: resultState={resultState != null}, allCorrect={allCorrect}");
            if (resultState.TitleText != null)
            {
                resultState.TitleText.SetText(allCorrect ? "Success" : "Review Results");
            }

            if (resultState.SummaryText != null)
            {
                resultState.SummaryText.SetText(
                    allCorrect
                        ? "All outputs matched the expected values."
                        : "Some outputs were incorrect or unstable.");
            }

            SetEnabledResultsGroup(resultState, true);
        }
    }
}
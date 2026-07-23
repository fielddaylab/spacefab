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
        public RectTransform VerticalLayoutToCopy;
        public RectTransform VerticalLayoutCopy;

        // One-frame intent flag: the player clicked "Continue" on a passing results panel. Consumed
        // by DesignContinueSystem, which decides whether to advance to the next level (reload the
        // Design scene) or return to overarching (last level). Mirrors the PlayFullSuiteRequested
        // hand-off OnRetryClicked uses, keeping scene-loading out of this UI click handler.
        public bool ContinueRequested;
        public bool CopyRequested;

        public void OnRegister()
        {

            if (ResultsGroup != null)
            {
                ResultStateUtility.SetEnabledResultsGroup(this, false);
            }

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
            // "Continue" on a passing panel: hand off to DesignContinueSystem, which advances to the
            // next level (Design-scene reload) or returns to overarching on the last level. A failing
            // panel just dismisses.
            Find.State(out SimulateRunState runState);
            runState.PlayFullSuiteRequested = false; // in case we were on a single-test pass
            if (ResultStateUtility.IsAllCorrect(runState))
            {
                ContinueRequested = true;
            }
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
            //resultState.CopyRequested = true;
        }

        public static void CopySimTable(ResultState resultState)
        {
            // first clear
            for (int i = 0; i < resultState.VerticalLayoutCopy.childCount; i++)
            {
                GameObject.Destroy(resultState.VerticalLayoutCopy.GetChild(i).gameObject);
            }

            // copy image graphics to result display
            for (int i = 0; i < resultState.VerticalLayoutToCopy.childCount; i++)
            {
                GameObject originalRow = resultState.VerticalLayoutToCopy.GetChild(i).gameObject;
                GameObject rowCopy = GameObject.Instantiate(originalRow, resultState.VerticalLayoutCopy);

            }
        }

        public static void ShowResults(ResultState resultState, bool allCorrect)
        {
            if (resultState.TitleText != null)
            {
                resultState.TitleText.SetText(allCorrect ? "Success" : "Failure");
                resultState.TitleText.color = allCorrect ? Color.green : Color.red;
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

        static public bool IsAllCorrect(SimulateRunState runState)
        {
            TestRowVerdict[] verdicts = runState.RowVerdicts;
            for (int i = 0; i < verdicts.Length; i++)
            {
                if (verdicts[i] != TestRowVerdict.Correct) { return false; }
            }
            return true;
        }
    }
}
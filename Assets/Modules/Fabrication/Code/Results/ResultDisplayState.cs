using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SpaceFab.Fabrication.Sequence;

namespace SpaceFab.Fabrication
{
    public class ResultDisplayState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool DisplayRequestedThisFrame;

        public CanvasGroup ResultsGroup;
        public Image Background;
        public DynamicButton RetryButton, ContinueButton;

        public ResultDisplaySection Heading, Accuracy, Time, ProductionTime;
        public StationResultDisplay[] StationResults;

        public Routine ResultsTransitionRoutine;

        public Sprite[] BackgroundSprites;
        public Sprite[] HeadingSprites;

        public void OnDeregister()
        {
            ResultsTransitionRoutine.Stop();

            RetryButton.onClick.RemoveAllListeners();
            ContinueButton.onClick.RemoveAllListeners();
        }

        public void OnRegister()
        {
            ResultDisplayStateUtility.SetEnabledResultsGroup(this, false);

            RetryButton.onClick.AddListener(OnRetryClicked);
            ContinueButton.onClick.AddListener(OnFinalizeClicked);
        }

        public void OnRetryClicked()
        {
            Debug.Log("Retry clicked!");
            
            Find.State<InterruptState>().ResetRequestedThisFrame = true;
            ResultDisplayStateUtility.SetEnabledResultsGroup(this, false);
        }

        public void OnFinalizeClicked()
        {
            Debug.Log("Finalize clicked!");
            Find.State<InterruptState>().FinalizeAttemptRequestedThisFrame = true;
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
            displayState.Accuracy.Text.text = "";
            displayState.Time.Text.text = "";
            displayState.ProductionTime.Text.text = "";
            foreach(StationResultDisplay station in displayState.StationResults)
                station.gameObject.SetActive(false);
            displayState.RetryButton.gameObject.SetActive(false);
            displayState.ContinueButton.gameObject.SetActive(false);

            // TODO: determine success/failure
            // Set display color
            WaferState waferState = Find.State<WaferState>();
            bool success = WaferStateUtility.GetAggregatedPrecision(waferState) > 0.8f;
            displayState.Heading.Text.text = success ? "WAFER COMPLETE" : "WAFER FAILED";
            displayState.Background.sprite = displayState.BackgroundSprites[success ? 0 : 1];
            displayState.Heading.Background.sprite = displayState.HeadingSprites[success ? 0 : 1];
            Color sectionColor = success ? new Color(127f / 255f, 205f / 255f, 154f / 255f) :
                new Color(244f / 255f, 150f / 255f, 121f / 255f);
            displayState.Accuracy.Background.color = sectionColor;
            displayState.Time.Background.color = sectionColor;
            displayState.ProductionTime.Background.color = sectionColor;

            yield return 1f;

            SetEnabledResultsGroup(displayState, true);
            
            yield return 0.5f;

            // Show ratings for each station
            Find.State(out SequenceState sequence);
            FabricationStep[] steps = sequence.Level.Sequence.Steps;
            float[] stationPrecisions = new float[displayState.StationResults.Length];
            float[] stationCount = new float[displayState.StationResults.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                stationPrecisions[(int)steps[i].StepId] += waferState.StepPrecisions[i];
                stationCount[(int)steps[i].StepId]++;
            }

            for (int i = 0; i < displayState.StationResults.Length; i++)
            {
                if (stationCount[i] == 0) {
                    displayState.StationResults[i].gameObject.SetActive(false);
                    continue;
                }

                float average = stationPrecisions[i] / stationCount[i];
                displayState.StationResults[i].SetRating(average);
                displayState.StationResults[i].gameObject.SetActive(true);
                yield return 0.25f;
            }

            // Show section
            TimeState timeState = Find.State<TimeState>();
            float time = TimeStateUtility.GetElapsed(timeState);
            FabricationMinigameState fabState = Find.State<FabricationMinigameState>();
            float secondssPerCycle = 30;
            int cycles = (int) Mathf.Ceil(time / secondssPerCycle);

            displayState.Accuracy.Text.text = $"{fabState.Precision * 100:F2}%";
            yield return 0.5f;

            displayState.Time.Text.text = $"{time:F2}s";
            yield return 0.5f;

            displayState.ProductionTime.Text.text = $"{fabState.TotalCycles} cycles";
            yield return 0.5f;

            // Show button
            GameObject button = success ? displayState.ContinueButton.gameObject : displayState.RetryButton.gameObject;
            button.SetActive(true);

            SpacefabGame.Events.Dispatch(GameEvents.FabSucceeded, EvtArgs.Create((fabState.Precision, time, cycles)));
            yield break;
        }

        private static IEnumerator HideResultsRoutine(ResultDisplayState displayState)
        {
            SetEnabledResultsGroup(displayState, false);
            yield break;
        }
    }
}

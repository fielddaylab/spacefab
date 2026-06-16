using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Stations;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpaceFab.Fabrication
{
    public class ResultDisplayState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool DisplayRequestedThisFrame;

        public CanvasGroup ResultsGroup;
        public DynamicButton RetryButton;
        public DynamicButton FinalizeButton;

        public TMP_Text AccuracyText, TimeText, ProductionTimeText;
        public StationResultDisplay[] StationResults;

        public Routine ResultsTransitionRoutine;

        public void OnDeregister()
        {
            ResultsTransitionRoutine.Stop();

            RetryButton.onClick.RemoveAllListeners();
            FinalizeButton.onClick.RemoveAllListeners();
        }

        public void OnRegister()
        {
            ResultDisplayStateUtility.SetEnabledResultsGroup(this, false);

            RetryButton.onClick.AddListener(OnRetryClicked);
            FinalizeButton.onClick.AddListener(OnFinalizeClicked);
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
            Debug.Log("Show results");
            
        }

        public static void HideResults(ResultDisplayState displayState)
        {
            displayState.ResultsTransitionRoutine.Replace(HideResultsRoutine(displayState));
        }

        private static IEnumerator ShowResultsRoutine(ResultDisplayState displayState)
        {
            displayState.AccuracyText.text = "";
            displayState.TimeText.text = "";
            displayState.ProductionTimeText.text = "";
            foreach(StationResultDisplay station in displayState.StationResults)
                station.gameObject.SetActive(false);

            yield return 1f;

            SetEnabledResultsGroup(displayState, true);

            yield return 0.5f;

            WaferState waferState = Find.State<WaferState>();
            for (int i = 0; i < waferState.RecordedStepCount; i++)
            {
                displayState.StationResults[i].SetRating(waferState.StepPrecisions[i]);
                displayState.StationResults[i].gameObject.SetActive(true);
            }

            yield return 0.5f;

            displayState.AccuracyText.text = $"{WaferStateUtility.GetAggregatedPrecision(waferState) * 100:F2}%";

            yield return 0.5f;

            TimeState timeState = Find.State<TimeState>();
            displayState.TimeText.text = $"{TimeStateUtility.GetElapsed(timeState):F2}s";

            yield return 0.5f;

            float secondssPerCycle = 30;
            displayState.ProductionTimeText.text = $"{Mathf.Ceil(TimeStateUtility.GetElapsed(timeState) / secondssPerCycle)} cycles";

            float accuracy = WaferStateUtility.GetAggregatedPrecision(waferState);
            float time = TimeStateUtility.GetElapsed(timeState);
            int cycles = (int) Mathf.Ceil(time / secondssPerCycle);
            SpacefabGame.Events.Dispatch(GameEvents.FabSucceeded, EvtArgs.Create((accuracy, time, cycles)));

            yield break;
        }

        private static IEnumerator HideResultsRoutine(ResultDisplayState displayState)
        {
            SetEnabledResultsGroup(displayState, false);
            yield break;
        }
    }
}

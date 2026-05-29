using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public class CountdownState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public bool CountdownRequestedThisFrame;
        [HideInInspector] public bool CountdownCompletedThisFrame;
        public TextMeshProUGUI CountDownText;
        public CanvasGroup CountDownCanvasGroup;
        [HideInInspector] public float AccruedTime;
        [HideInInspector] public bool IsCountingDown;

        public Routine CountdownRoutine;

        public void OnDeregister()
        {
            CountdownRoutine.Stop();
        }

        public void OnRegister()
        {
            CountDownCanvasGroup.alpha = 0f;
            CountDownCanvasGroup.blocksRaycasts = false;
            CountDownCanvasGroup.interactable = false;
        }
    }

    public static class CountdownUtility
    {
        public static void BeginCountdown(CountdownState countdownState)
        {
            countdownState.CountdownRoutine.Replace(CountdownRoutine(countdownState));

            Find.State(out CountdownState state);

            state.CountDownCanvasGroup.alpha = 1f;
            state.CountDownCanvasGroup.blocksRaycasts = true;
            state.CountDownCanvasGroup.interactable = true;
        }

        public static IEnumerator CountdownRoutine(CountdownState countdownState)
        {
            countdownState.IsCountingDown = true;
            countdownState.AccruedTime = 0;
            yield return 4; // set to 4 since 3 seconds for 3-2-1 and 1 sec for 'GO!'
            yield return RoutinePhase.LateUpdate;
            GameLoop.QueuePreUpdate(SignalCountdownCompleted);
        }

        private static void SignalCountdownCompleted()
        {
            Find.State(out CountdownState state);

            state.IsCountingDown = false;
            state.CountdownCompletedThisFrame = true;

            state.CountDownCanvasGroup.alpha = 0f;
            state.CountDownCanvasGroup.blocksRaycasts = false;
            state.CountDownCanvasGroup.interactable = false;
        }
    }
}

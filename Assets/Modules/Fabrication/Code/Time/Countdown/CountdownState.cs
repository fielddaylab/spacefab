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
    public class CountdownState : SharedStateComponent, IRegistrationCallbacks
    {
        public bool CountdownRequestedThisFrame;
        public bool CountdownCompletedThisFrame;

        public Routine CountdownRoutine;

        public void OnDeregister()
        {
            CountdownRoutine.Stop();

        }

        public void OnRegister()
        {
        }
    }

    public static class CountdownUtility
    {
        public static void BeginCountdown(CountdownState countdownState)
        {
            countdownState.CountdownRoutine.Replace(CountdownRoutine(countdownState));
        }

        public static IEnumerator CountdownRoutine(CountdownState countdownState)
        {
            yield return 3;
            yield return RoutinePhase.LateUpdate;
            GameLoop.QueuePreUpdate(() => { SignalCountdownCompleted(countdownState); });
        }

        private static void SignalCountdownCompleted(CountdownState countdownState)
        {
            countdownState.CountdownCompletedThisFrame = true;
        }
    }
}

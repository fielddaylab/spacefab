using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Holds data for the attempt stopwatch. Ticks up from 0 for the duration of the attempt.
    /// Gets reset to 0 on reset. Pauses on pause, request exit system, and on results screen.
    /// </summary>
    public class TimeState : SharedStateComponent, IRegistrationCallbacks
    {
        // UI element to update with current time
        [SerializeField] public TextMeshProUGUI TimerText;
        // flag to disable incrementing the timer
        [HideInInspector] public bool IsPaused = false;
        // storing elapsed time of a current run
        [HideInInspector] public float ElapsedTime;

        public void OnRegister()
        {
            TimerText.text = "00.00";
        }

        public void OnDeregister()
        {

        }
    }

    /// <summary>
    /// Queries and commands on the stopwatch. Used by the sequence system to snapshot elapsed time
    /// at a checkpoint and to roll it back on restore. Scaffold stubs; real values wire in when
    /// TimeState fields are defined.
    /// </summary>
    public static class TimeStateUtility
    {
        // Returns the elapsed time since the attempt began in seconds
        public static float GetElapsed(TimeState state)
        {
            return state.ElapsedTime;
        }

        // Sets the elapsed time. Used by checkpoint restoration to roll the stopwatch back to the
        // value captured at the checkpoint.
        public static void SetElapsed(TimeState state, float value)
        {
            state.ElapsedTime = value;
        }

        public static void ResetTime(TimeState state)
        {
            state.ElapsedTime = 0;
        }
    }
}

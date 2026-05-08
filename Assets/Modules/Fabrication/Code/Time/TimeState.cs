using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Holds data for the attempt stopwatch. Ticks up from 0 for the duration of the attempt.
    /// Gets reset to 0 on reset. Pauses on pause, request exit system, and on results screen.
    /// </summary>
    public class TimeState : SharedStateComponent
    {
        // TODO: stopwatch fields (elapsed time, paused flag, etc.).
    }

    /// <summary>
    /// Queries and commands on the stopwatch. Used by the sequence system to snapshot elapsed time
    /// at a checkpoint and to roll it back on restore. Scaffold stubs; real values wire in when
    /// TimeState fields are defined.
    /// </summary>
    public static class TimeStateUtility
    {
        // Returns the elapsed time since the attempt began (in whatever unit TimeState uses).
        // Scaffold default: 0.
        public static float GetElapsed(TimeState state)
        {
            // TODO: return state.<elapsed-field>.
            return 0f;
        }

        // Sets the elapsed time. Used by checkpoint restoration to roll the stopwatch back to the
        // value captured at the checkpoint.
        public static void SetElapsed(TimeState state, float value)
        {
            // TODO: assign value onto state.
        }
    }
}

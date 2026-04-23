using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Holds data regarding the timer. Gets reset to 0 on reset.
    /// Pauses on pause, request exit system, and on results screen.
    /// </summary>
    public class TimeState : SharedStateComponent
    {
        // TODO: timer fields (cycles remaining, paused flag, etc.).
    }

    /// <summary>
    /// Queries and commands on the timer. Used by the sequence system to snapshot and restore time
    /// at checkpoint steps. Scaffold stubs; real values wire in when TimeState fields are defined.
    /// </summary>
    public static class TimeStateUtility
    {
        // Returns the current remaining time (in whatever unit TimeState uses). Scaffold default: 0.
        public static float GetRemaining(TimeState state)
        {
            // TODO: return state.<remaining-field>.
            return 0f;
        }

        // Sets the remaining time. Used by checkpoint restoration to roll back the clock.
        public static void SetRemaining(TimeState state, float value)
        {
            // TODO: assign value onto state.
        }
    }
}

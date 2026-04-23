using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public enum LevelMode
    {
        PreAttempt,
        AttemptLeadIn,
        Attempt,
        PostAttempt
    }

    /// <summary>
    /// Holds data for the current Fabrication Mode.
    /// Includes Pre-Attempt, Attempt, and Post-Attempt modes.
    /// </summary>
    public class ModeState : SharedStateComponent
    {
        public LevelMode CurrMode;
        public bool ChangedModeThisFrame; // TODO ModeStateRefreshSystem
    }

    public static class ModeUtility
    {
        public static void SetNewMode(ModeState modeState, LevelMode newMode)
        {
            if (newMode == modeState.CurrMode) { return; }

            modeState.CurrMode = newMode;
            modeState.ChangedModeThisFrame = true;
        }
    }
}
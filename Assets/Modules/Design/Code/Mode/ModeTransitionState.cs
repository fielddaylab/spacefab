using FieldDay;
using FieldDay.Systems;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SpaceFab.Design
{
    /// <summary>
    /// Which Design-minigame mode is currently active. Drives ModeTransitionSystem's decision
    /// on whether a play request needs a Tool→Simulate transition (with graph rebuild) or is
    /// already inside Simulate mode and just needs the request to flow through.
    /// </summary>
    public enum DesignMode
    {
        Tool,
        Simulate,
    }

    /// <summary>
    /// Holds data relevant to transitioning between Design minigame modes.
    /// Modes include Tool Mode and Simulate Mode.
    /// </summary>
    public class ModeTransitionState : SharedStateComponent, IRegistrationCallbacks
    {
        // Defaults to Tool — DesignMinigameState.DefaultUpdateMask matches (ToolModeMask active,
        // SimulateModeMask suspended).
        [NonSerialized] public DesignMode Mode;

        public void OnRegister()
        {
            Mode = DesignMode.Tool;
        }

        public void OnDeregister()
        {
        }
    }
}
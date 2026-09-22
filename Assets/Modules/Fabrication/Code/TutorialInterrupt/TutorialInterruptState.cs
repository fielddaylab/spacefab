using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Flags driving the microgame retry popup. Raised when a completion fails the precision gate
    /// (StationControlUtility.SetCompletionVerdict) and when the restart button is pressed; consumed by
    /// TutorialInterruptSystem and cleared by TutorialInterruptRefreshSystem. The popup widgets themselves
    /// live on MicrogameCanvasState — this state holds only the interrupt's control flags.
    /// </summary>
    public class TutorialInterruptState : SharedStateComponent, IRegistrationCallbacks
    {
        // One-frame request to open the popup. Set on a precision-gate failure; consumed to show the popup
        // and pause the timer; cleared by TutorialInterruptRefreshSystem.
        [NonSerialized] public bool TutorialInterruptRequested;

        // Latch raised by the StationRestartButton click listener; consumed-and-cleared by
        // TutorialInterruptSystem (which restarts the microgame and hides the popup). NOT cleared by the
        // refresh system: the click is async to the frame, so a fixed-boundary clear could drop it before
        // the system sees it.
        [NonSerialized] public bool RestartButtonPressedThisFrame;

        // Persistent guard so TutorialInterruptSystem registers the restart-button listener exactly once
        // per scene. NOT cleared by the refresh system.
        [NonSerialized] public bool ListenerRegistered;

        public void OnRegister()
        {
            TutorialInterruptRequested = false;
            RestartButtonPressedThisFrame = false;
            ListenerRegistered = false;
        }

        public void OnDeregister()
        {
        }
    }
}

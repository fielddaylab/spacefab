using BeauUtil;
using FieldDay.Components;
using SpaceFab.Fabrication.StationControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Stations {
    /// <summary>
    /// Lifecycle phase of the station interfacer, driven by the station-control state machine.
    /// </summary>
    public enum MicrogameInterfacerPhase {
        Idle,       // Not in use; no microgame active.
        Entering,   // State machine has begun entry; interfacer is running its intro.
        Active,     // Microgame owns input; derived internal sub-phases are the microgame's concern.
        Exiting,    // State machine has begun exit (normal or cancel); interfacer is tearing down.
    }

    /// <summary>
    /// Per-station bridge between the station-control state machine and a concrete microgame implementation.
    /// Holds a reference to an IMicrogame and routes lifecycle callbacks through it.
    /// </summary>
    public class MicrogameStationInterfacer : BatchedComponent {
        // Well-known station identifier, authored per-station on the prefab. Used by the sequence
        // system to match steps to stations and to recognize the universal Defrag station.
        [SerializeField] private SerializedHash32 m_Id;
        public SerializedHash32 Id => m_Id;

        [HideInInspector] public MicrogameInterfacerPhase Phase;

        // Set by the microgame (via MicrogameStationInterfacerUtility.SignalCompleted) when its internal
        // state machine reaches "done". Forwarded same frame to StationControlState.MicrogameCompletedThisFrame
        // by MicrogameStationInterfacerBridgeSystem; cleared at end of frame by MicrogameStationInterfacerRefreshSystem.
        [HideInInspector] public bool CompletedThisFrame;

        // Set by the microgame (via MicrogameStationInterfacerUtility.SignalProcessAnimationStarted) when
        // it starts a parallel process animation during InMicrogame. Forwarded same frame to
        // StationControlState.ProcessAnimationInProgress by MicrogameStationInterfacerBridgeSystem;
        // cleared at end of frame by MicrogameStationInterfacerRefreshSystem.
        [HideInInspector] public bool ProcessAnimationStartedThisFrame;

        // The microgame component hosted at this station. Must implement IMicrogame. Optional: when null,
        // the station has no microgame and Activate attempts will no-op.
        [SerializeField] private MonoBehaviour m_MicrogameComponent;

        public IMicrogame Microgame => m_MicrogameComponent as IMicrogame;
    }

    /// <summary>
    /// Paired utility for MicrogameStationInterfacer. The state-machine lifecycle methods (BeginEnter,
    /// EnterComplete, BeginExit, ExitComplete) are called by StationControlSystem on phase transitions.
    /// The Signal* methods are called by concrete microgames and only mutate the interfacer —
    /// MicrogameStationInterfacerBridgeSystem is responsible for forwarding their flags into
    /// StationControlState on the same frame.
    /// </summary>
    public static class MicrogameStationInterfacerUtility {
        // Called by StationControlSystem on AtStation -> EnteringMicrogame.
        public static void BeginEnter(MicrogameStationInterfacer interfacer) {
            interfacer.Phase = MicrogameInterfacerPhase.Entering;
            interfacer.Microgame?.OnEnterBegin();
        }

        // Called by StationControlSystem on EnteringMicrogame -> InMicrogame.
        public static void EnterComplete(MicrogameStationInterfacer interfacer) {
            interfacer.Phase = MicrogameInterfacerPhase.Active;
            interfacer.Microgame?.OnEnterComplete();
        }

        // Called by StationControlSystem on InMicrogame -> ExitingMicrogame. completedNormally = false
        // when the player cancelled. Raises ProcessAnimationInProgress on a normal completion (idempotent —
        // the microgame may already have started one in parallel) and drops it on cancel.
        public static void BeginExit(MicrogameStationInterfacer interfacer, StationControlState stationState, bool completedNormally) {
            interfacer.Phase = MicrogameInterfacerPhase.Exiting;
            interfacer.Microgame?.OnExitBegin(completedNormally);
            if (completedNormally) {
                // Idempotent — preserve a flag the microgame already raised in parallel.
                if (!stationState.ProcessAnimationInProgress) {
                    stationState.ProcessAnimationInProgress = true;
                }
            } else {
                // Cancel drops any in-flight animation; the exit timer runs unblocked.
                stationState.ProcessAnimationInProgress = false;
            }
        }

        // Resets the microgame to a fresh active play state without replaying the intro transition.
        // Used by StationControlUtility.RestartMicrogame: runs the enter lifecycle hooks back-to-back so
        // the microgame behaves as if just entered and immediately owns input again.
        public static void Reenter(MicrogameStationInterfacer interfacer) {
            BeginEnter(interfacer);
            EnterComplete(interfacer);
        }

        // Called by StationControlSystem on ExitingMicrogame -> AtStation.
        public static void ExitComplete(MicrogameStationInterfacer interfacer) {
            interfacer.Phase = MicrogameInterfacerPhase.Idle;
            interfacer.Microgame?.OnExitComplete();
        }

        // Called by the concrete microgame when its internal state is ready to exit normally. Only
        // mutates the interfacer's one-frame flag; MicrogameStationInterfacerBridgeSystem forwards
        // it to StationControlState.MicrogameCompletedThisFrame the same frame.
        public static void SignalCompleted(MicrogameStationInterfacer interfacer) {
            interfacer.CompletedThisFrame = true;
        }

        // Called by a concrete microgame to begin a "process animation" that plays in parallel
        // with the active microgame. Only mutates the interfacer's one-frame flag;
        // MicrogameStationInterfacerBridgeSystem forwards it to StationControlState the same frame
        // and the bridge / NotifyProcessAnimationStarted is what enforces the "only while InMicrogame"
        // guard.
        public static void SignalProcessAnimationStarted(MicrogameStationInterfacer interfacer) {
            interfacer.ProcessAnimationStartedThisFrame = true;
        }
    }
}

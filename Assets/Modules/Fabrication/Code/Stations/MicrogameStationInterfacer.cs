using BeauUtil;
using FieldDay.Components;
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
        // state machine reaches "done". Consumed same frame by StationControlSystem.
        [HideInInspector] public bool CompletedThisFrame;

        // The microgame component hosted at this station. Must implement IMicrogame. Optional: when null,
        // the station has no microgame and Activate attempts will no-op.
        [SerializeField] private MonoBehaviour m_MicrogameComponent;

        public IMicrogame Microgame => m_MicrogameComponent as IMicrogame;
    }

    /// <summary>
    /// Paired utility for MicrogameStationInterfacer. Invoked by StationControlSystem on each state
    /// machine transition, and by concrete microgames signaling their completion.
    /// </summary>
    public static class MicrogameStationInterfacerUtility {
        // Called by StationControlSystem on AtStation -> EnteringMicrogame.
        public static void BeginEnter(MicrogameStationInterfacer interfacer) {
            // TODO: set Phase = Entering; invoke interfacer.Microgame?.OnEnterBegin().
        }

        // Called by StationControlSystem on EnteringMicrogame -> InMicrogame.
        public static void EnterComplete(MicrogameStationInterfacer interfacer) {
            // TODO: set Phase = Active; invoke interfacer.Microgame?.OnEnterComplete().
        }

        // Called by StationControlSystem on InMicrogame -> ExitingMicrogame. completedNormally = false on cancel from StationControlUtility.RequestCancel()
        public static void BeginExit(MicrogameStationInterfacer interfacer, bool completedNormally) {
            // TODO: set Phase = Exiting; invoke interfacer.Microgame?.OnExitBegin(completedNormally).
            //       StationControlState stationState = Find.State<StationControlState>().
            //       If completedNormally: if (!stationState.ProcessAnimationInProgress) stationState.ProcessAnimationInProgress = true.
            //         (Idempotent — leave true if the microgame already started one in parallel.)
            //       Else (cancel): stationState.ProcessAnimationInProgress = false. Drops any in-flight animation.
        }

        // Called by StationControlSystem on ExitingMicrogame -> AtStation.
        public static void ExitComplete(MicrogameStationInterfacer interfacer) {
            // TODO: set Phase = Idle; invoke interfacer.Microgame?.OnExitComplete().
        }

        // Called by the concrete microgame when its internal state is ready to exit normally.
        // Sets the one-frame flag AND notifies the shared state, so StationControlSystem picks it up this frame.
        public static void SignalCompleted(MicrogameStationInterfacer interfacer) {
            // TODO: set interfacer.CompletedThisFrame = true; call StationControlUtility.NotifyMicrogameCompleted.
        }

        // Called by a concrete microgame to begin a "process animation" that plays in parallel
        // with the active microgame. Once raised, the station-control machine will hold
        // ExitingMicrogame (when the microgame eventually completes successfully) until
        // IsProcessAnimationComplete() returns true OR the player presses Skip. No-op if not
        // currently in InMicrogame, so a stray call after exit can't accidentally re-arm the flag.
        public static void SignalProcessAnimationStarted(MicrogameStationInterfacer interfacer) {
            // TODO: StationControlState stationState = Find.State<StationControlState>().
            //       if (stationState.Phase == StationControlPhase.InMicrogame) stationState.ProcessAnimationInProgress = true.
        }
    }
}

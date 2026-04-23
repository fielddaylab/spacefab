using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using SpaceFab.Fabrication.Movement;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.StationControl {
    /// <summary>
    /// Phase of the station-control state machine. Governs what input is accepted each frame.
    /// </summary>
    public enum StationControlPhase {
        Traveling,          // Robot moving between slots; movement live, Activate ignored.
        AtStation,          // Robot parked at a slot; movement live, Activate accepted.
        EnteringMicrogame,  // Intro animation running; all input blocked.
        InMicrogame,        // Microgame owns input; Cancel accepted at any sub-phase.
        ExitingMicrogame,   // Outro animation running; all input blocked.
        Stunned,            // Wrong-station penalty; all input blocked; returns to PostStunPhase when done.
    }

    /// <summary>
    /// Tracks the robot's within-Attempt interaction phase. Decides which inputs are legal each frame
    /// and flips MicrogameMask on/off when entering/leaving InMicrogame. Does not mirror slot position —
    /// consumers read MovementState directly.
    /// </summary>
    public class StationControlState : SharedStateComponent, IRegistrationCallbacks {
        [HideInInspector] public StationControlPhase Phase;

        // The station currently being interacted with. Non-null in AtStation / EnteringMicrogame /
        // InMicrogame / ExitingMicrogame; null otherwise.
        [HideInInspector] public MicrogameStationInterfacer ActiveInterfacer;

        // Accumulates wall time within timed phases (EnteringMicrogame, ExitingMicrogame, Stunned).
        public float PhaseTimer;

        // Inspector-editable global durations.
        public float EnterMicrogameDuration = 0.5f;
        public float ExitMicrogameDuration = 0.5f;
        public float StunDuration = 1.5f;

        // One-frame flags, cleared by StationControlFlagRefreshSystem in LateUpdate.
        [HideInInspector] public bool MicrogameCompletedThisFrame;
        [HideInInspector] public bool CancelRequestedThisFrame;

        // Phase to resume after Stunned clears. Set when TriggerStun is invoked.
        [HideInInspector] public StationControlPhase PostStunPhase;

        public void OnRegister() {
            Phase = StationControlPhase.Traveling;
            ActiveInterfacer = null;
            PhaseTimer = 0f;
            MicrogameCompletedThisFrame = false;
            CancelRequestedThisFrame = false;
            PostStunPhase = StationControlPhase.Traveling;
        }

        public void OnDeregister() {
        }
    }

    /// <summary>
    /// Queries and commands for StationControlState. The command methods are the Leaf integration surface
    /// (add [LeafMember] attributes when Leaf scripting is wired in).
    /// </summary>
    public static class StationControlUtility {
        // ---- Queries (consumed by MovementSystem / WorldInteractSystem for input gating) ----

        // True when movement input should be processed (Traveling or AtStation).
        public static bool AllowsMovement(StationControlState stationState) {
            // TODO: return Phase == Traveling || Phase == AtStation.
            return false;
        }

        // True when an Activate press should be forwarded to RequestActivate (AtStation only).
        public static bool AllowsActivate(StationControlState stationState) {
            // TODO: return Phase == AtStation.
            return false;
        }

        // True when a Cancel press should be forwarded to RequestCancel (InMicrogame only).
        public static bool AllowsCancel(StationControlState stationState) {
            // TODO: return Phase == InMicrogame.
            return false;
        }

        // True when the state machine considers a microgame to currently own input.
        public static bool IsMicrogameOwned(StationControlState stationState) {
            // TODO: return Phase == InMicrogame.
            return false;
        }

        // ---- Commands (Leaf-integration surface) ----

        // Called by WorldInteractSystem when the player presses Activate at a station.
        // Routes to either EnteringMicrogame (if CanActivateNow) or Stunned (otherwise).
        public static void RequestActivate(StationControlState stationState, MicrogameStationInterfacer interfacer) {
            // TODO: if Phase != AtStation, no-op. If interfacer/Microgame null or !CanActivateNow, TriggerStun(AtStation).
            // Otherwise: ActiveInterfacer = interfacer; BeginEnter(interfacer); PhaseTimer=0; Phase = EnteringMicrogame;
            // dispatch FabStationEnterBegin.
        }

        // Called by WorldInteractSystem when the player presses Cancel. Sets the one-frame flag.
        public static void RequestCancel(StationControlState stationState) {
            // TODO: if Phase == InMicrogame, set CancelRequestedThisFrame = true.
        }

        // Called by MicrogameStationInterfacerUtility.SignalCompleted when a microgame finishes naturally.
        public static void NotifyMicrogameCompleted(StationControlState stationState) {
            // TODO: if Phase == InMicrogame, set MicrogameCompletedThisFrame = true.
        }

        // Forces transition into Stunned for StunDuration seconds. After the stun, machine returns to returnPhase.
        public static void TriggerStun(StationControlState stationState, StationControlPhase returnPhase) {
            // TODO: PostStunPhase = returnPhase; PhaseTimer = 0; Phase = Stunned; dispatch FabStunBegin.
            // Caller is responsible for applying the RobotState stun via RobotUtility.ApplyStun.
        }
    }
}

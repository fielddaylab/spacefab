using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Stations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// Top-level status of the sequence state machine. Orthogonal to the station-control phase;
    /// the two machines compose (sequence runs within Attempt while station-control runs within
    /// each step's station interaction).
    /// </summary>
    public enum SequenceStatus
    {
        Idle,       // Before the first Attempt starts, or after the level ends cleanly.
        Active,     // Player is progressing through steps normally.
        Halted,     // Misalignment detected and no checkpoint available. Awaiting full ResetSequence.
        Restoring,  // Checkpoint rollback in progress (lead-in coroutine running).
        Completed   // All steps completed. Awaiting level-end flow (results screen etc.).
    }

    /// <summary>
    /// Per-step runtime data, parallel to FabricationSequenceLevel.Steps. Rebuilt on every
    /// ResetSequence call. Holds state the level asset itself cannot carry (it is immutable).
    /// </summary>
    [Serializable]
    public struct StepRuntimeData
    {
        // True if this step's hint card is currently glitched. Set at reset time per the level's
        // GlitchMode; cleared by SequenceUtility.UnglitchCurrentStep when the player completes the
        // Defrag microgame.
        public bool IsGlitched;

        // True once the player has reached (completed) this step AND it was flagged as a checkpoint
        // in the level definition. Used by diagnostic / UI to show reached checkpoints.
        public bool WasCheckpointReached;
    }

    /// <summary>
    /// Snapshot of game state captured when a checkpoint step completes. On a later misalignment,
    /// SequenceUtility.RestoreCheckpoint writes these values back onto TimeState, WaferState, and
    /// MovementState, then plays the lead-in and resumes. Valid only when SequenceState.HasCheckpoint
    /// is true.
    /// </summary>
    [Serializable]
    public struct SequenceCheckpoint
    {
        // Index of the checkpoint step within FabricationSequenceLevel.Steps. The sequence resumes
        // at StepIndex + 1 on restore.
        public int StepIndex;

        // Stopwatch reading at the moment the checkpoint step completed (elapsed time since the
        // attempt began). Unit matches TimeStateUtility.GetElapsed (TBD when TimeState is defined).
        public float TimeElapsed;

        // Wafer snapshot at the moment the checkpoint step completed. Restored via
        // WaferStateUtility.RestoreSnapshot.
        public WaferSnapshot WaferSnapshot;

        // MovementState.CurrSlotPosition at the moment the checkpoint step completed. Restored by
        // writing back onto MovementState and flagging SlotChangedThisFrame so StationControlSystem
        // re-parks the robot at the matching AtStation.
        public int SlotIndex;
    }

    /// <summary>
    /// Tracks progression through a fabrication sequence: the current step pointer, per-step
    /// runtime data, and the most-recently-reached checkpoint snapshot. Advanced by SequenceSystem
    /// in response to station-control events; reset by SequenceUtility.ResetSequence from three
    /// external callers (Attempt start, player reset, results retry).
    /// </summary>
    public class SequenceState : SharedStateComponent, IRegistrationCallbacks
    {
        [HideInInspector] public SequenceStatus Status;

        // The level asset this sequence is running. Assigned by the level-load flow
        // (FabricationMinigameState.ImportState -> SequenceUtility.ResetSequence).
        [HideInInspector] public FabricationLevel Level;

        // 0-based index into Level.Steps. Valid only when Status is Active. When Status is
        // Restoring, CurrentStepIndex has already been rolled back to the step after the checkpoint.
        [HideInInspector] public int CurrentStepIndex;

        // Per-step runtime data (IsGlitched, WasCheckpointReached). Rebuilt on ResetSequence.
        [HideInInspector] public StepRuntimeData[] StepRuntime;

        // Populated by SequenceUtility.CaptureCheckpoint when a checkpoint step completes. Read by
        // SequenceUtility.RestoreCheckpoint on misalignment. The Checkpoint fields are valid only
        // when HasCheckpoint is true.
        [HideInInspector] public bool HasCheckpoint;
        [HideInInspector] public SequenceCheckpoint Checkpoint;

        // BeauRoutine handle for the lead-in coroutine played during checkpoint restoration.
        public Routine RestoreRoutine;

        // One-frame flag: a misalignment was detected this frame. Set by SequenceUtility
        // .FlagMisalignment, cleared by SequenceFlagRefreshSystem in LateUpdate.
        [HideInInspector] public bool MisalignmentThisFrame;

        public void OnRegister()
        {
            Status = SequenceStatus.Idle;
            Level = null;
            CurrentStepIndex = 0;
            StepRuntime = null;
            HasCheckpoint = false;
            Checkpoint = default;
            MisalignmentThisFrame = false;
        }

        public void OnDeregister()
        {
            RestoreRoutine.Stop();
        }
    }
}

using BeauUtil;
using FieldDay;
using SpaceFab.Fabrication.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// Queries and commands for SequenceState. The command methods are the Leaf integration surface
    /// (add [LeafMember] attributes when Leaf scripting is wired in).
    /// </summary>
    public static class SequenceUtility
    {
        // ---- Queries ----

        // Returns the step the player should currently be working on, or null if out of range
        // (i.e. sequence is Idle, Halted, Completed, or CurrentStepIndex is past the last step).
        public static FabricationStep? GetCurrentStep(SequenceState sequenceState)
        {
            // TODO: bounds-check CurrentStepIndex against Level.Steps.Length; return by value.
            return null;
        }

        // True when the sequence is actively accepting step progression (Status == Active).
        public static bool IsActive(SequenceState sequenceState)
        {
            // TODO: return sequenceState.Status == SequenceStatus.Active.
            return false;
        }

        // True when the station id matches the universal Defrag station.
        public static bool IsDefragStation(StringHash32 stationId)
        {
            // TODO: return stationId == FabricationConsts.DEFRAG_STATION_ID.
            return false;
        }

        // ---- Commands (reset / progression) ----

        // Resets the sequence to step 0 using the supplied level asset. Called at Attempt start,
        // on player-initiated reset, and on results-retry. Rebuilds StepRuntime (including a fresh
        // glitch roll based on GlitchMode), clears any captured checkpoint, sets Status = Active,
        // dispatches FabSequenceReset.
        public static void ResetSequence(SequenceState sequenceState, FabricationSequenceLevel level)
        {
            // TODO:
            //   sequenceState.Level = level
            //   sequenceState.CurrentStepIndex = 0
            //   sequenceState.StepRuntime = new StepRuntimeData[level.Steps.Length]
            //   RollGlitches(sequenceState, level)
            //   sequenceState.HasCheckpoint = false
            //   sequenceState.Checkpoint = default
            //   // Allocate per-step precision storage on the wafer. Indices align 1:1 with Level.Steps.
            //   WaferState waferState = Find.State<WaferState>()
            //   waferState.StepPrecisions = new float[level.Steps.Length]
            //   waferState.RecordedStepCount = 0
            //   sequenceState.Status = SequenceStatus.Active
            //   Game.Events.Dispatch(GameEvents.FabSequenceReset)
        }

        // Called by SequenceSystem when a microgame completes at a station matching the current
        // step AND the wafer matches the expected snapshot. Captures a checkpoint if this step was
        // a checkpoint, increments the step pointer, transitions to Completed on the last step,
        // dispatches FabSequenceStepCompleted (and FabSequenceCompleted on final step).
        public static void AdvanceStep(SequenceState sequenceState, WaferState waferState, TimeState timeState, MovementState movementState)
        {
            // TODO:
            //   int justCompleted = sequenceState.CurrentStepIndex
            //   FabricationStep step = sequenceState.Level.Steps[justCompleted]
            //   if (step.IsCheckpoint) CaptureCheckpoint(sequenceState, waferState, timeState, movementState, justCompleted)
            //   sequenceState.CurrentStepIndex++
            //   if (sequenceState.CurrentStepIndex >= sequenceState.Level.Steps.Length) {
            //       sequenceState.Status = SequenceStatus.Completed
            //       Game.Events.Dispatch(GameEvents.FabSequenceCompleted)
            //   }
            //   Game.Events.Dispatch(GameEvents.FabSequenceStepCompleted)
        }

        // Called by SequenceSystem when the wafer snapshot at step-end doesn't match expected, OR
        // when the activated station was not the expected station for the current step. Sets the
        // MisalignmentThisFrame flag, dispatches FabWaferMisalignment. If a checkpoint is available,
        // rolls back; otherwise transitions to Halted (awaiting full reset).
        public static void FlagMisalignment(SequenceState sequenceState)
        {
            // TODO:
            //   sequenceState.MisalignmentThisFrame = true
            //   Game.Events.Dispatch(GameEvents.FabWaferMisalignment)
            //   if (sequenceState.HasCheckpoint) RestoreCheckpoint(sequenceState)
            //   else sequenceState.Status = SequenceStatus.Halted
        }

        // Called by SequenceSystem when the player arrives at the Defrag station. Clears the
        // glitch flag on the current step's card. Does not advance the step or run alignment check.
        public static void UnglitchCurrentStep(SequenceState sequenceState)
        {
            // TODO:
            //   if (!IsActive(sequenceState)) return
            //   if (CurrentStepIndex out of bounds) return
            //   sequenceState.StepRuntime[sequenceState.CurrentStepIndex].IsGlitched = false
            //   Game.Events.Dispatch(GameEvents.FabStepUnglitched)
        }

        // ---- Checkpoint machinery ----

        // Snapshots time / wafer / slot at the moment a checkpoint step is completed. Sets
        // HasCheckpoint = true and marks the step as having been reached.
        public static void CaptureCheckpoint(SequenceState sequenceState, WaferState waferState, TimeState timeState, MovementState movementState, int stepIndex)
        {
            // TODO:
            //   sequenceState.HasCheckpoint = true
            //   sequenceState.Checkpoint = new SequenceCheckpoint {
            //       StepIndex = stepIndex,
            //       TimeElapsed = TimeStateUtility.GetElapsed(timeState),
            //       WaferSnapshot = WaferStateUtility.TakeSnapshot(waferState),
            //       SlotIndex = movementState.CurrSlotPosition,
            //   }
            //   sequenceState.StepRuntime[stepIndex].WasCheckpointReached = true
            //   Game.Events.Dispatch(GameEvents.FabCheckpointReached)
        }

        // Rolls back to the captured checkpoint. Sets Status = Restoring, advances CurrentStepIndex
        // to the step after the checkpoint, starts the RestoreRoutine.
        public static void RestoreCheckpoint(SequenceState sequenceState)
        {
            // TODO:
            //   sequenceState.Status = SequenceStatus.Restoring
            //   sequenceState.CurrentStepIndex = sequenceState.Checkpoint.StepIndex + 1
            //   Game.Events.Dispatch(GameEvents.FabCheckpointRestoreBegin)
            //   sequenceState.RestoreRoutine.Replace(RestoreCoroutine(sequenceState))
        }

        // The coroutine body for a checkpoint restore: writes snapshot values back onto runtime
        // state, runs the lead-in pause, then re-arms the sequence.
        public static IEnumerator RestoreCoroutine(SequenceState sequenceState)
        {
            // TODO:
            //   WaferState waferState = Find.State<WaferState>()
            //   TimeState timeState = Find.State<TimeState>()
            //   MovementState movementState = Find.State<MovementState>()
            //   ref SequenceCheckpoint cp = ref sequenceState.Checkpoint
            //   WaferStateUtility.RestoreSnapshot(waferState, cp.WaferSnapshot)
            //   TimeStateUtility.SetElapsed(timeState, cp.TimeElapsed)
            //   movementState.CurrSlotPosition = cp.SlotIndex
            //   movementState.SlotChangedThisFrame = true   // so StationControlSystem re-parks at AtStation
            //   yield return RestoreLeadIn()
            //   sequenceState.Status = SequenceStatus.Active
            //   Game.Events.Dispatch(GameEvents.FabCheckpointRestoreComplete)
            yield break;
        }

        // Game-wide lead-in played after a checkpoint rollback restores state and before the
        // sequence resumes. Identical across levels. Scaffold stub plays a fixed-duration pause;
        // replace with fade-from-black / "Resuming..." text / countdown / SFX when UI is wired in.
        public static IEnumerator RestoreLeadIn()
        {
            // TODO: fade from black, show "Resuming..." text, play SFX, countdown.
            yield return FabricationConsts.CHECKPOINT_LEAD_IN_SECONDS;
        }

        // ---- Glitch machinery ----

        // Rebuilds StepRuntime[].IsGlitched based on the level's GlitchMode. Called from ResetSequence.
        public static void RollGlitches(SequenceState sequenceState, FabricationSequenceLevel level)
        {
            // TODO:
            //   switch level.GlitchMode:
            //     Explicit:   for each i in level.GlitchedStepIndices, StepRuntime[i].IsGlitched = true
            //     Percentage: for each step, StepRuntime[i].IsGlitched = UnityEngine.Random.value < level.GlitchChance
        }
    }
}

using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Point-in-time capture of wafer state, used by the sequence system to compare the current wafer
    /// against an expected result after each step, and to restore the wafer on checkpoint rollback.
    /// Includes the per-step precision array so rolled-back precisions are discarded alongside
    /// wafer geometry. Scaffold-only; wafer-geometry fields will grow when the wafer data model is designed.
    /// </summary>
    [Serializable]
    public struct WaferSnapshot
    {
        // TODO: actual wafer geometry fields (layers, patterns, rotation, materials). Placeholder
        // below is a version stamp so callers can construct non-default instances during scaffold testing.
        public int PlaceholderVersion;

        // Per-step precision scores at the moment of the snapshot. Deep-copied on capture/restore
        // so the snapshot is an independent value. Indices align 1:1 with FabricationSequenceLevel.Steps.
        public float[] StepPrecisions;

        // Number of step precisions actually written so far (entries [0, RecordedStepCount) in
        // StepPrecisions are valid; the rest are default(float)).
        public int RecordedStepCount;
    }

    /// <summary>
    /// Holds data regarding player's wafer (whether newly-minted, in-progress, or post-attempt)
    /// Wafers are constructed of layers with patterns, rotation, and materials.
    /// Checked against the target wafer state to evaluate results.
    /// Tracks per-step precision as microgames complete and exposes an aggregated average for results.
    /// </summary>
    public class WaferState : SharedStateComponent
    {
        // TODO: fields for layers, patterns, rotation, materials.

        // Per-step precision scores. Allocated by SequenceUtility.ResetSequence with length equal to
        // the number of steps in the level. Written by microgames on successful completion via
        // WaferStateUtility.SetStepPrecision; read by results UI and by GetAggregatedPrecision.
        [HideInInspector] public float[] StepPrecisions;

        // Number of entries in StepPrecisions that have been written by microgames this attempt.
        // Used for aggregation so uncompleted steps don't drag the average toward 0.
        [HideInInspector] public int RecordedStepCount;
    }

    /// <summary>
    /// Snapshot / comparison / precision utilities used by the sequence system and microgames.
    /// Scaffold stubs; MatchesSnapshot returns true by default so the sequence machine runs
    /// end-to-end while the wafer geometry model is undefined.
    /// </summary>
    public static class WaferStateUtility
    {
        // Captures the current wafer state into a snapshot for later comparison or restoration.
        // Deep-copies StepPrecisions so mutating the wafer later doesn't corrupt the snapshot.
        public static WaferSnapshot TakeSnapshot(WaferState state)
        {
            // TODO: copy wafer geometry fields into the snapshot.
            // TODO: snapshot.StepPrecisions = (state.StepPrecisions != null) ? (float[])state.StepPrecisions.Clone() : null;
            // TODO: snapshot.RecordedStepCount = state.RecordedStepCount;
            return default;
        }

        // Restores the wafer to a previously captured snapshot. Used by checkpoint rollback.
        // Deep-copies the snapshot's StepPrecisions back onto state so subsequent writes don't
        // mutate the captured snapshot.
        public static void RestoreSnapshot(WaferState state, WaferSnapshot snapshot)
        {
            // TODO: write snapshot geometry fields back onto state.
            // TODO: state.StepPrecisions = (snapshot.StepPrecisions != null) ? (float[])snapshot.StepPrecisions.Clone() : null;
            // TODO: state.RecordedStepCount = snapshot.RecordedStepCount;
        }

        // Compares the current wafer to an expected snapshot. Returns true when they match, meaning
        // the sequence step's postcondition was satisfied. Returns true by default so the scaffold
        // runs without wafer data; real equality logic lands when WaferState has real fields.
        public static bool MatchesSnapshot(WaferState current, WaferSnapshot expected)
        {
            // TODO: compare real wafer geometry fields. StepPrecisions are explicitly NOT part of
            // the match check; they're outcome data, not postcondition data.
            return true;
        }

        // Records this step's precision. Called by a microgame from OnExitBegin(completedNormally: true).
        // Updates RecordedStepCount to the max of itself and (stepIndex + 1) so aggregation scopes
        // to the steps actually attempted.
        public static void SetStepPrecision(WaferState state, int stepIndex, float precision)
        {
            // TODO:
            //   bounds-check stepIndex against state.StepPrecisions.Length
            //   state.StepPrecisions[stepIndex] = precision
            //   if (stepIndex >= state.RecordedStepCount) state.RecordedStepCount = stepIndex + 1
        }

        // Returns the average of all precisions recorded so far (entries [0, RecordedStepCount)).
        // Returns 0 when nothing has been recorded. Consumed by results display and
        // FabricationMinigameState.ExportState for save.
        public static float GetAggregatedPrecision(WaferState state)
        {
            // TODO:
            //   if (state.RecordedStepCount <= 0) return 0f
            //   sum state.StepPrecisions[0 .. RecordedStepCount) and divide by RecordedStepCount
            return 0f;
        }
    }
}

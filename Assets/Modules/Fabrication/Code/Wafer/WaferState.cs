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
    /// Scaffold-only placeholder; fields will grow when the wafer data model is designed.
    /// </summary>
    [Serializable]
    public struct WaferSnapshot
    {
        // TODO: actual wafer fields (layers, patterns, rotation, materials). Placeholder below is a
        // version stamp so callers can construct non-default instances during scaffold testing.
        public int PlaceholderVersion;
    }

    /// <summary>
    /// Holds data regarding player's wafer (whether newly-minted, in-progress, or post-attempt)
    /// Wafers are constructed of layers with patterns, rotation, and materials.
    /// Checked against the target wafer state to evaluate results
    /// </summary>
    public class WaferState : SharedStateComponent
    {
        // TODO: fields for layers, patterns, rotation, materials.
    }

    /// <summary>
    /// Snapshot and comparison utilities used by the sequence system. All methods are scaffold stubs;
    /// MatchesSnapshot returns true by default so the sequence machine can run end-to-end while the
    /// wafer model is undefined.
    /// </summary>
    public static class WaferStateUtility
    {
        // Captures the current wafer state into a snapshot for later comparison or restoration.
        public static WaferSnapshot TakeSnapshot(WaferState state)
        {
            // TODO: copy wafer fields into the snapshot.
            return default;
        }

        // Restores the wafer to a previously captured snapshot. Used by checkpoint rollback.
        public static void RestoreSnapshot(WaferState state, WaferSnapshot snapshot)
        {
            // TODO: write snapshot fields back onto state.
        }

        // Compares the current wafer to an expected snapshot. Returns true when they match, meaning
        // the sequence step's postcondition was satisfied. Returns true by default so the scaffold
        // runs without wafer data; real equality logic lands when WaferState has real fields.
        public static bool MatchesSnapshot(WaferState current, WaferSnapshot expected)
        {
            // TODO: compare real wafer fields.
            return true;
        }
    }
}

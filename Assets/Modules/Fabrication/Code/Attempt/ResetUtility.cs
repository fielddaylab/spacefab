using FieldDay;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public static class ResetUtility
    {
        // TODO: clear wafer state, reset timer, etc.
        public static void ResetAttempt()
        {
            Find.State(
                out WaferState waferState,
                out TimeState timeState,
                out SequenceState sequenceState,
                out SequenceVisualsState sequenceVisuals
            );
            Find.State(
                out ModeState modeState
                );

            TimeStateUtility.ResetTime(timeState);
            WaferStateUtility.ResetWafer(waferState);

            // at end ask sequence to reset
            SequenceUtility.ResetSequence(sequenceState, sequenceState.Level, sequenceVisuals);
        }
    }
}
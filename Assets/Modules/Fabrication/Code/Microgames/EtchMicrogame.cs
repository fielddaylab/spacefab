using FieldDay;
using FieldDay.Components;
using SpaceFab.Fabrication.Sequence;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Plasma Etcher station microgame: "Etch-a-sketch." Players steer a plasma beam across the
    /// developed photoresist pattern with arrow keys; the interaction ends when the beam exits the
    /// wafer. Precision is accuracy against the target pattern.
    /// </summary>
    public class EtchMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: play intro; spawn plasma beam at pattern entry.
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting directional input; begin beam travel.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze beam.
            if (!completedNormally) { return; }

            float precision = ComputePrecision();
            Find.State(out WaferState waferState, out SequenceState sequenceState);
            WaferStateUtility.SetStepPrecision(waferState, sequenceState.CurrentStepIndex, precision);
        }

        public void OnExitComplete()
        {
            // TODO: tear down beam UI; return to idle.
        }

        // Etch-a-sketch-specific precision math: fraction of target-pattern cells the beam
        // correctly traversed, minus cells incorrectly traversed. Scaffold returns 0.
        private float ComputePrecision()
        {
            // TODO: precision = (correctCells - incorrectCells) / targetCells, clamped to [0,1].
            return 0f;
        }
    }
}

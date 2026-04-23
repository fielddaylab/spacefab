using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Furnace microgame: "Thermometer." Players hold Activate to raise a Heat value to a target
    /// range for Oxidation / N-Type Doping / P-Type Doping. Precision is heat-difference from
    /// target range center.
    /// </summary>
    public class FurnaceMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: play intro (station name flash, spawn heat dial UI).
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting Activate-hold input; begin heat simulation.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze heat simulation.
            if (!completedNormally) { return; }

            MicrogameUtility.CommitStepPrecision(ComputePrecision());
        }

        public void OnExitComplete()
        {
            // TODO: tear down heat dial UI; return to idle.
        }

        // Furnace-specific precision math: difference between final heat value and the target
        // range center. Scaffold returns 0.
        private float ComputePrecision()
        {
            // TODO: precision = 1 - (abs(finalHeat - targetCenter) / targetHalfWidth), clamped to [0,1].
            return 0f;
        }
    }
}

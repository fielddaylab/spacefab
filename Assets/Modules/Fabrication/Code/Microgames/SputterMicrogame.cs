using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Sputtering station microgame: "Spraypaint." Players hold Activate to sputter material onto
    /// the exposed etched pattern, moving the head with arrow keys. Player manually ends the
    /// interaction. Precision is percent of the etched area filled.
    /// </summary>
    public class SputterMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: play intro; spawn sputter head above wafer.
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting directional + activate input; enable sputter painting.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze head.
            if (!completedNormally) { return; }

            MicrogameUtility.CommitStepPrecision(ComputePrecision());
        }

        public void OnExitComplete()
        {
            // TODO: tear down sputter UI; return to idle.
        }

        // Spraypaint-specific precision math: percent of the etched target area that got filled
        // by the sputter head. Scaffold returns 0.
        private float ComputePrecision()
        {
            // TODO: precision = filledCells / targetCells, clamped to [0,1].
            return 0f;
        }
    }
}

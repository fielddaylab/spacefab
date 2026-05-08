using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Ion Implanter station microgame: ____.
    /// Precision is ____.
    /// </summary>
    public class IonMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: 
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting input.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze dropper.
            if (!completedNormally) { return; }

            MicrogameUtility.CommitStepPrecision(ComputePrecision());
        }

        // TODO: track process animation state (parallel or sequential) and return true once the
        // animation has finished playing. Scaffold returns true so the exit gate doesn't stall
        // before per-microgame animations are authored.
        public bool IsProcessAnimationComplete()
        {
            return true;
        }

        public void OnExitComplete()
        {
            // TODO: tear down dropper UI; return to idle.
        }

        // 
        // Scaffold returns 0.
        private float ComputePrecision()
        {
            // TODO: precision = ????
            return 0f;
        }
    }
}

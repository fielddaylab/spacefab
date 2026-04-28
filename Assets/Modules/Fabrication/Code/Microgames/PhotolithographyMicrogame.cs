using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Photolithography station microgame: "Mask Drop." A photomask falls onto the wafer; players
    /// rotate it with left/right and can speed the fall with down. Precision is angle-difference
    /// from target orientation when the mask lands.
    /// </summary>
    public class PhotolithographyMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: play intro; spawn photomask above wafer; begin slow fall.
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting rotate + accelerate input.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze mask position.
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
            // TODO: tear down photomask UI; return to idle.
        }

        // Mask-Drop-specific precision math: angle delta from target orientation at landing.
        // Scaffold returns 0.
        private float ComputePrecision()
        {
            // TODO: precision = 1 - (abs(finalAngle - targetAngle) / 180f), clamped to [0,1].
            return 0f;
        }
    }
}

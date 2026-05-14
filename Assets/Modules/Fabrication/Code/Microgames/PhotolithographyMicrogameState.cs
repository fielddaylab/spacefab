using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Holds in-flight data for the Photolithography ("Mask Drop") microgame: the photomask's
    /// current rotation and fall state, the target landing orientation, and lifecycle flags
    /// consumed by PhotolithographyMicrogameSystem.
    /// </summary>
    public class PhotolithographyMicrogameState : SharedStateComponent
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // PhotolithographyMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;

        // TODO: photomask current angle, current fall position / velocity, target angle.
    }

    /// <summary>
    /// Paired utility for PhotolithographyMicrogameState. Drives the Photolithography microgame's
    /// lifecycle hooks invoked from PhotolithographyMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class PhotolithographyMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out PhotolithographyMicrogameState state);
            state.IsActive = true;
            // TODO: play intro; spawn photomask above wafer; begin slow fall.
        }

        public static void EnterComplete()
        {
            // TODO: start accepting rotate + accelerate input.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            // TODO: freeze mask position.
            if (!completedNormally) { return; }

            MicrogameUtility.CommitStepPrecision(ComputePrecision());
        }

        // TODO: track process animation state (parallel or sequential) and return true once the
        // animation has finished playing. Scaffold returns true so the exit gate doesn't stall
        // before per-microgame animations are authored.
        public static bool IsProcessAnimationComplete()
        {
            return true;
        }

        public static void ExitComplete()
        {
            Find.State(out PhotolithographyMicrogameState state);
            state.IsActive = false;
            // TODO: tear down photomask UI; return to idle.
        }

        // Mask-Drop-specific precision math: angle delta from target orientation at landing.
        // Scaffold returns 0.
        private static float ComputePrecision()
        {
            // TODO: precision = 1 - (abs(finalAngle - targetAngle) / 180f), clamped to [0,1].
            return 0f;
        }
    }
}

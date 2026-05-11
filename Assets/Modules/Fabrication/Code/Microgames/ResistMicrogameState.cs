using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Holds in-flight data for the Photoresist ("Spin-Coat") microgame: the dropper's sweep
    /// position, the recorded drop offset from center, and lifecycle flags consumed by
    /// ResistMicrogameSystem.
    /// </summary>
    public class ResistMicrogameState : SharedStateComponent
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // ResistMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;

        // TODO: dropper sweep X position, drop X position (recorded on Activate press), max offset.
    }

    /// <summary>
    /// Paired utility for ResistMicrogameState. Drives the Photoresist microgame's lifecycle hooks
    /// invoked from ResistMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class ResistMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out ResistMicrogameState state);
            state.IsActive = true;
            // TODO: play intro; spawn dropper; begin left-right sweep.
        }

        public static void EnterComplete()
        {
            // TODO: start accepting Activate-press input.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            // TODO: freeze dropper.
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
            Find.State(out ResistMicrogameState state);
            state.IsActive = false;
            // TODO: tear down dropper UI; return to idle.
        }

        // Spin-Coat-specific precision math: distance between drop position and wafer center.
        // Scaffold returns 0.
        private static float ComputePrecision()
        {
            // TODO: precision = 1 - (abs(dropX - centerX) / maxOffset), clamped to [0,1].
            return 0f;
        }
    }
}

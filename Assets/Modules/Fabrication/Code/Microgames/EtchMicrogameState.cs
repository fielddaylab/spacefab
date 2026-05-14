using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Holds in-flight data for the Plasma Etcher ("Etch-a-sketch") microgame: the beam's
    /// position over the pattern, the per-cell correct/incorrect tally, and lifecycle flags
    /// consumed by EtchMicrogameSystem.
    /// </summary>
    public class EtchMicrogameState : SharedStateComponent
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // EtchMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;

        // TODO: beam position, correctCells / incorrectCells / targetCells counters for precision math.
    }

    /// <summary>
    /// Paired utility for EtchMicrogameState. Drives the Plasma Etcher microgame's lifecycle hooks
    /// invoked from EtchMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class EtchMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out EtchMicrogameState state);
            state.IsActive = true;
            // TODO: play intro; spawn plasma beam at pattern entry.
        }

        public static void EnterComplete()
        {
            // TODO: start accepting directional input; begin beam travel.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            // TODO: freeze beam.
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
            Find.State(out EtchMicrogameState state);
            state.IsActive = false;
            // TODO: tear down beam UI; return to idle.
        }

        // Etch-a-sketch-specific precision math: fraction of target-pattern cells the beam
        // correctly traversed, minus cells incorrectly traversed. Scaffold returns 0.
        private static float ComputePrecision()
        {
            // TODO: precision = (correctCells - incorrectCells) / targetCells, clamped to [0,1].
            return 0f;
        }
    }
}

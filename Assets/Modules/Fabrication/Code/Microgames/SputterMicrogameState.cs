using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public enum SputterMicrogamePhase
    {
        Idle,
        Active,
        Exiting
    }
    
    /// <summary>
    /// Holds in-flight data for the Sputter ("Spraypaint") microgame: the sputter head's position,
    /// the fill state of the etched target area, and lifecycle flags consumed by SputterMicrogameSystem.
    /// </summary>
    public class SputterMicrogameState : SharedStateComponent
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // SputterMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;
        [HideInInspector] public bool InputAccepted;
        public GameObject SputterUI;
        public SputterMicrogamePhase Phase;

        public LineRenderer IncidentBeam;
        public LineRenderer[] ReflectedBeam;
        public Transform ReflectionPoint;

        // TODO: head position, head velocity / input delta.
        // TODO: fill grid (filledCells / targetCells) for precision math.
    }

    /// <summary>
    /// Paired utility for SputterMicrogameState. Drives the Sputter microgame's lifecycle hooks
    /// invoked from SputterMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class SputterMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out SputterMicrogameState state);
            state.IsActive = true;
            // TODO: play intro; spawn sputter head above wafer.
        }

        public static void EnterComplete()
        {
            // TODO: start accepting directional + activate input; enable sputter painting.
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            // TODO: freeze head.
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
            Find.State(out SputterMicrogameState state);
            state.IsActive = false;
            // TODO: tear down sputter UI; return to idle.
        }

        // Spraypaint-specific precision math: percent of the etched target area that got filled
        // by the sputter head. Scaffold returns 0.
        private static float ComputePrecision()
        {
            // TODO: precision = filledCells / targetCells, clamped to [0,1].
            return 0f;
        }
    }
}

using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public enum SputterMicrogamePhase
    {
        Entering,
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
        public Transform SputterSprites;
        public SpriteRenderer PatternRenderer;

        public float MaxSputterDistance = 1.75f;
    }

    /// <summary>
    /// Paired utility for SputterMicrogameState. Drives the Sputter microgame's lifecycle hooks
    /// invoked from SputterMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class SputterMicrogameUtility
    {
        // determines if microgame can be started based on if this step is next
        public static bool CanActivate()
        {
            Find.State(out SequenceState state);
            return SequenceUtility.CheckNextStep(state, FabricationConsts.SPUTTER_STATION_ID);
        }

        public static void EnterBegin()
        {
            Find.State(out SputterMicrogameState state);

            state.Phase = SputterMicrogamePhase.Entering;
            state.IsActive = true;
            state.InputAccepted = false;
            state.SputterUI.SetActive(true);
        }

        public static void EnterComplete()
        {
            Find.State(out SputterMicrogameState state);

            state.Phase = SputterMicrogamePhase.Active;
            state.InputAccepted = true;
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            Find.State(out SputterMicrogameState state);
            state.Phase = SputterMicrogamePhase.Exiting;
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
            Find.State(out SputterMicrogameState state, out MicrogameCanvasState canvasState);

            state.IsActive = false;
            state.Phase = SputterMicrogamePhase.Idle;
            state.SputterUI.SetActive(false);

            MicrogameCanvasUtility.HideStationInstructions(canvasState);
            // TODO: tear down sputter UI; return to idle.
        }

        // Side-effect-free precision query for the precision gate, read before ExitBegin commits.
        public static float GetResultPrecision()
        {
            return ComputePrecision();
        }

        // Sputter error is unsigned (fill percent), so raw equals the gate precision.
        public static float GetRawResultPrecision()
        {
            return ComputePrecision();
        }

        // Spraypaint-specific precision math: percent of the etched target area that got filled
        // by the sputter head. Scaffold returns 0.
        private static float ComputePrecision()
        {
            Find.State(out SputterMicrogameState state);

            float precision = 1 - (state.MaxSputterDistance - state.SputterSprites.localPosition.x) / state.MaxSputterDistance;
            return Mathf.Clamp01(precision);
        }
    }
}

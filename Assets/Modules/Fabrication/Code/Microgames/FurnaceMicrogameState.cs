using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public enum FurnaceMicrogamePhase
    {
        Idle,       // awaiting start
        Entering,   // entering microgame
        Fueling,   // waiting for player to hold button
        Burning,  // animating meter moving into place
        Exiting     // cleanup
    }
    
    /// <summary>
    /// Holds in-flight data for the Furnace ("Thermometer") microgame: the current heat value,
    /// the target range for the active step's process, and lifecycle flags consumed by
    /// FurnaceMicrogameSystem.
    /// </summary>
    public class FurnaceMicrogameState : SharedStateComponent, IRegistrationCallbacks
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // FurnaceMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;

        // TODO: current heat value, target range center + half-width (varies per process: Oxidation /
        // N-Type Doping / P-Type Doping).
        public float TargetRange, MaxRange, HalfWidth, Sensitivity;
        public float CurrentValue;

        public bool InputAccepted;

        public GameObject FurnaceUI;

        public FurnaceMicrogamePhase Phase;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            // Disable UI on start
            FurnaceUI.SetActive(false);
        }
    }

    /// <summary>
    /// Paired utility for FurnaceMicrogameState. Drives the Furnace microgame's lifecycle hooks
    /// invoked from FurnaceMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class FurnaceMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out FurnaceMicrogameState state);
            state.IsActive = true;
            // TODO: play intro (station name flash, spawn heat dial UI).
            state.FurnaceUI.SetActive(true);
            state.Phase = FurnaceMicrogamePhase.Entering;
        }

        public static void EnterComplete()
        {
            Find.State(out FurnaceMicrogameState state);
            
            // TODO: start accepting Activate-hold input; begin heat simulation.
            state.InputAccepted = true;
            state.Phase = FurnaceMicrogamePhase.Burning;
        }

        // On normal completion, compute precision and commit it to the wafer at the current step.
        // On cancel, nothing is recorded.
        public static void ExitBegin(bool completedNormally)
        {
            // TODO: freeze heat simulation.
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
            Find.State(out FurnaceMicrogameState state);
            state.IsActive = false;
            // TODO: tear down heat dial UI; return to idle.
        }

        // Furnace-specific precision math: difference between final heat value and the target
        // range center. Scaffold returns 0.
        private static float ComputePrecision()
        {
            // TODO: precision = 1 - (abs(finalHeat - targetCenter) / targetHalfWidth), clamped to [0,1].
            return 0f;
        }
    }
}

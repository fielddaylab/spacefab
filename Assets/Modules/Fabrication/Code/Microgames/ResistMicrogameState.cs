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
        [HideInInspector] public float SweeperX, DropX;

        public float CenterX, MaxOffset, SweepSpeed;

        public bool InputAccepted;

        public GameObject ResistUI;
        public Transform SweeperGraphic;
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
            state.ResistUI.SetActive(true);

        }

        public static void EnterComplete()
        {
            // TODO: start accepting Activate-press input.
            Find.State(out ResistMicrogameState state);
            state.InputAccepted = true;
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
            state.ResistUI.SetActive(false);
        }

        // Spin-Coat-specific precision math: distance between drop position and wafer center.
        private static float ComputePrecision()
        {
            Find.State(out ResistMicrogameState state);

            float precision = 1 - (Mathf.Abs(state.DropX - state.CenterX) / state.MaxOffset);
            precision = Mathf.Clamp(precision, 0f, 1f);

            return precision;
        }
    }
}

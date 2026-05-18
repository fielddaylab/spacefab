using FieldDay;
using FieldDay.SharedState;
using SpaceFab.Fabrication.Layout;
using SpaceFab.Fabrication.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Holds in-flight data for the Ion Implanter microgame, and lifecycle flags consumed by
    /// IonMicrogameSystem. Mechanics are not yet specified.
    /// </summary>
    public class IonMicrogameState : SharedStateComponent, IRegistrationCallbacks
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // IonMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;
        public GameObject IonUI;
        // TODO: dropper position and any other simulation fields once mechanics are defined.

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            // Disable UI on start
            IonUI.SetActive(false);
        }
    }

    /// <summary>
    /// Paired utility for IonMicrogameState. Drives the Ion Implanter microgame's lifecycle hooks
    /// invoked from IonMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class IonMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: gate based on sequence / wafer state. Default true.
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(
                out IonMicrogameState state,
                out MicrogameCanvasState canvasState
                );
            state.IsActive = true;
            // TODO:

            state.IonUI.SetActive(true);
            canvasState.ShowUI(FabricationConsts.ION_STATION_ID);
        }

        public static void EnterComplete()
        {
            // TODO: start accepting input.
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
            Find.State(out IonMicrogameState state);
            state.IsActive = false;
            // TODO: tear down dropper UI; return to idle.
        }

        // Scaffold returns 0 until precision math is defined.
        private static float ComputePrecision()
        {
            // TODO: precision = ????
            return 0f;
        }
    }
}

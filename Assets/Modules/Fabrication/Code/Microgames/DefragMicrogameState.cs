using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Holds in-flight data for the Defrag microgame: the meter fill level versus its decay,
    /// and lifecycle flags consumed by DefragMicrogameSystem. Universal escape hatch for glitched
    /// sequence steps; does not record precision.
    /// </summary>
    public class DefragMicrogameState : SharedStateComponent
    {
        // True while this microgame owns input/simulation. Set by EnterBegin, cleared by ExitComplete.
        // DefragMicrogameSystem reads this to gate its ProcessWork.
        [HideInInspector] public bool IsActive;

        // TODO: meter fill level [0,1] and decay rate.
    }

    /// <summary>
    /// Paired utility for DefragMicrogameState. Drives the Defrag microgame's lifecycle hooks
    /// invoked from DefragMicrogame (the Unity-side IMicrogame component).
    /// </summary>
    public static class DefragMicrogameUtility
    {
        public static bool CanActivate()
        {
            // TODO: Defrag is always activatable (it's the escape hatch for glitched steps).
            return true;
        }

        public static void EnterBegin()
        {
            Find.State(out DefragMicrogameState state);
            state.IsActive = true;
            // TODO: play intro; spawn Defrag meter UI.
        }

        public static void EnterComplete()
        {
            // TODO: start accepting Activate-mash input; begin meter decay.
        }

        // Unglitch is dispatched by SequenceSystem on completedNormally; nothing to commit here.
        public static void ExitBegin(bool completedNormally)
        {
            // TODO: freeze meter.
        }

        // Defrag has no precision concept (it's the universal escape hatch), so it always reports a
        // perfect score and passes any precision gate the Leaf author may apply to it.
        public static float GetResultPrecision()
        {
            return 1f;
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
            Find.State(out DefragMicrogameState state);
            state.IsActive = false;
            // TODO: tear down Defrag meter UI; return to idle.
        }
    }
}

using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Defragmentation station microgame. Universal: visiting and completing this microgame
    /// unglitches the current sequence step's card (handled by SequenceSystem on
    /// FabMicrogameCompleted). Does not advance the sequence or run an alignment check.
    /// Player mashes Activate to fill the Defrag meter against its decay.
    /// </summary>
    public class DefragMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow()
        {
            // TODO: Defrag is always activatable (it's the escape hatch for glitched steps).
            return true;
        }

        public void OnEnterBegin()
        {
            // TODO: play intro; spawn Defrag meter UI.
        }

        public void OnEnterComplete()
        {
            // TODO: start accepting Activate-mash input; begin meter decay.
        }

        public void OnExitBegin(bool completedNormally)
        {
            // TODO: freeze meter. Unglitch is dispatched by SequenceSystem on completedNormally; nothing to do here.
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
            // TODO: tear down Defrag meter UI; return to idle.
        }
    }
}

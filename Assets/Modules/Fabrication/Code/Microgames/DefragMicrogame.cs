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
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// DefragMicrogameState / DefragMicrogameUtility / DefragMicrogameSystem.
    /// </summary>
    public class DefragMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => DefragMicrogameUtility.CanActivate();
        public void OnEnterBegin() => DefragMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => DefragMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => DefragMicrogameUtility.ExitBegin(completedNormally);
        public float GetResultPrecision() => DefragMicrogameUtility.GetResultPrecision();
        public bool IsProcessAnimationComplete() => DefragMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => DefragMicrogameUtility.ExitComplete();
    }
}

using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Photoresist station microgame: "Spin-Coat." A dropper above the wafer moves left-right;
    /// players press Activate to drop photoresist as close to the wafer's center as possible.
    /// Precision is distance-from-center.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// ResistMicrogameState / ResistMicrogameUtility / ResistMicrogameSystem.
    /// </summary>
    public class ResistMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => ResistMicrogameUtility.CanActivate();
        public void OnEnterBegin() => ResistMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => ResistMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => ResistMicrogameUtility.ExitBegin(completedNormally);
        public bool IsProcessAnimationComplete() => ResistMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => ResistMicrogameUtility.ExitComplete();
    }
}

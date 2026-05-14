using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Ion Implanter station microgame: ____.
    /// Precision is ____.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// IonMicrogameState / IonMicrogameUtility / IonMicrogameSystem.
    /// </summary>
    public class IonMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => IonMicrogameUtility.CanActivate();
        public void OnEnterBegin() => IonMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => IonMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => IonMicrogameUtility.ExitBegin(completedNormally);
        public bool IsProcessAnimationComplete() => IonMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => IonMicrogameUtility.ExitComplete();
    }
}

using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Photolithography station microgame: "Mask Drop." A photomask falls onto the wafer; players
    /// rotate it with left/right and can speed the fall with down. Precision is angle-difference
    /// from target orientation when the mask lands.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// PhotolithographyMicrogameState / PhotolithographyMicrogameUtility / PhotolithographyMicrogameSystem.
    /// </summary>
    public class PhotolithographyMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => PhotolithographyMicrogameUtility.CanActivate();
        public void OnEnterBegin() => PhotolithographyMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => PhotolithographyMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => PhotolithographyMicrogameUtility.ExitBegin(completedNormally);
        public float GetResultPrecision() => PhotolithographyMicrogameUtility.GetResultPrecision();
        public bool IsProcessAnimationComplete() => PhotolithographyMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => PhotolithographyMicrogameUtility.ExitComplete();
    }
}

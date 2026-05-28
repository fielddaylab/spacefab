using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Furnace microgame: "Thermometer." Players hold Activate to raise a Heat value to a target
    /// range for Oxidation / N-Type Doping / P-Type Doping. Precision is heat-difference from
    /// target range center.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// FurnaceMicrogameState / FurnaceMicrogameUtility / FurnaceMicrogameSystem.
    /// </summary>
    public class FurnaceMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => FurnaceMicrogameUtility.CanActivate();
        public void OnEnterBegin() => FurnaceMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => FurnaceMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => FurnaceMicrogameUtility.ExitBegin(completedNormally);
        public float GetResultPrecision() => FurnaceMicrogameUtility.GetResultPrecision();
        public bool IsProcessAnimationComplete() => FurnaceMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => FurnaceMicrogameUtility.ExitComplete();
    }
}

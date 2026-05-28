using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Plasma Etcher station microgame: "Etch-a-sketch." Players steer a plasma beam across the
    /// developed photoresist pattern with arrow keys; the interaction ends when the beam exits the
    /// wafer. Precision is accuracy against the target pattern.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// EtchMicrogameState / EtchMicrogameUtility / EtchMicrogameSystem.
    /// </summary>
    public class EtchMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => EtchMicrogameUtility.CanActivate();
        public void OnEnterBegin() => EtchMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => EtchMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => EtchMicrogameUtility.ExitBegin(completedNormally);
        public float GetResultPrecision() => EtchMicrogameUtility.GetResultPrecision();
        public bool IsProcessAnimationComplete() => EtchMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => EtchMicrogameUtility.ExitComplete();
    }
}

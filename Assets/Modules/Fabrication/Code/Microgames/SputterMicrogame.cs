using FieldDay.Components;
using SpaceFab.Fabrication.Stations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    /// <summary>
    /// Sputtering station microgame: "Spraypaint." Players hold Activate to sputter material onto
    /// the exposed etched pattern, moving the head with arrow keys. Player manually ends the
    /// interaction. Precision is percent of the etched area filled.
    ///
    /// Unity-side handle for the IMicrogame interface; logic and data live in
    /// SputterMicrogameState / SputterMicrogameUtility / SputterMicrogameSystem.
    /// </summary>
    public class SputterMicrogame : BatchedComponent, IMicrogame
    {
        public bool CanActivateNow() => SputterMicrogameUtility.CanActivate();
        public void OnEnterBegin() => SputterMicrogameUtility.EnterBegin();
        public void OnEnterComplete() => SputterMicrogameUtility.EnterComplete();
        public void OnExitBegin(bool completedNormally) => SputterMicrogameUtility.ExitBegin(completedNormally);
        public bool IsProcessAnimationComplete() => SputterMicrogameUtility.IsProcessAnimationComplete();
        public void OnExitComplete() => SputterMicrogameUtility.ExitComplete();
    }
}

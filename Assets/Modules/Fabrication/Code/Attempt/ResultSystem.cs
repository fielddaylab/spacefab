using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Manages results display after an attempt is completed
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhase.Update, 1, UpdateMasks.PostAttemptMask)]
    public class ResultSystem : SharedStateSystemBehaviour<WaferState>
    {
        static private void ProcessWork(float deltaTime) {
            GetDependencies();
        }

        protected override unsafe delegate*<float, void> GetDelegate() {
            return &ProcessWork;
        }
    }
}
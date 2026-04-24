using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    public static class ResetUtility
    {
        // TODO: clear wafer state, reset timer, etc.
        public static void ResetAttempt()
        {
            Find.State(
                out WaferState waferState,
                out TimeState timeState
            );
        }
    }
}
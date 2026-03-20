using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum OverarchingShutdownPhase
    {
        Waiting,
        ShuttingDown,
        ShutdownComplete
    }

    public class OverarchingShutdownSequenceState : SharedStateComponent
    {
        public OverarchingShutdownPhase Phase;
    }
}

using BeauRoutine;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum OverarchingShutdownPhase
    {
        Waiting,
        BeginShutdown,
        ShuttingDown,
        ShutdownComplete
    }

    public class OverarchingShutdownSequenceState : SharedStateComponent
    {
        public OverarchingShutdownPhase Phase;

        public Routine ShutdownRoutine;
    }
}

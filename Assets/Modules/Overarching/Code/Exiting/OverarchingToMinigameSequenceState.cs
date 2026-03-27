using BeauRoutine;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum OverarchingToMinigamePhase
    {
        Waiting,
        Starting,
        ShutdownSequenceSystem,
        TransitionToMinigame,
        TransitionComplete
    }

    public class OverarchingToMinigameSequenceState : SharedStateComponent
    {
        public OverarchingToMinigamePhase Phase;
    }
}

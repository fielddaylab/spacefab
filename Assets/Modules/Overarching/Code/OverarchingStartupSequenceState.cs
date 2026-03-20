using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum OverarchingStartupSequencePhase
    {
        DetermineSequence,
        ChapterLoad,
        ContractCompletionSystem,
        ContractSelectSystem,
        Completed,
    }

    public class OverarchingStartupSequenceState : SharedStateComponent
    {
        public OverarchingStartupSequencePhase Phase;
        public bool CompleteAfterLoad;
    }
}
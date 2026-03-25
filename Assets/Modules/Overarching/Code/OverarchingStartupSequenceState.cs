using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum OverarchingStartupSequencePhase
    {
        // If not already loaded
        LoadCurrChapter,

        // If coming from prev chapter
        ContractCompletionSystem,

        // Always
        LoadCurrAvailableContracts,

        // If curr contract not yet selected
        ContractSelectSystem,

        // Always
        LoadSelectedContract,
        Completed,
    }

    public class OverarchingStartupSequenceState : SharedStateComponent
    {
        public OverarchingStartupSequencePhase Phase;
    }
}
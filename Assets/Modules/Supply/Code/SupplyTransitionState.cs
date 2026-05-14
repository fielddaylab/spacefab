using BeauRoutine;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Supply
{
    public enum SupplyTransitionPhase
    {
        LoadingChapterMap,
        Completed
    }

    public class SupplyTransitionState : SharedStateComponent
    {
        public SupplyTransitionPhase Phase;
        public Routine LoadRoutine;
    }
}

using BeauRoutine;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace SpaceFab.Supply
{
    public enum SupplyTransitionPhase
    {
        LoadingChapterMap,
        Completed
    }

    public class SupplyTransitionState : SharedStateComponent, IRegistrationCallbacks, ISceneLoadDependency
    {
        public SupplyTransitionPhase Phase;
        public Routine LoadRoutine;

        public bool IsLoaded(SceneLoadFence fence) {
            return !LoadRoutine;
        }

        public void OnDeregister() {
            Game.Scenes.DeregisterLoadDependency(this);
        }

        public void OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }
    }
}

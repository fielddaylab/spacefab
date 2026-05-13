using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Design {
    /// <summary>
    /// Processes result display requests for Simulate mode.
    /// Reads/writes ResultState and shows the results panel when requested.
    /// </summary>
    public class ResultSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0),
                new SysPermissions()
                    .ReadWriteShared<ResultState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(out ResultState resultState);

            if (resultState.DisplayRequestedThisFrame) {
                resultState.DisplayRequestedThisFrame = false;
                ResultStateUtility.ShowResults(resultState, resultState.AllCorrect);
            }
        }
    }
}

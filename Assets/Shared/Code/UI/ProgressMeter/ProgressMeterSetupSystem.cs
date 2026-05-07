using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// </summary>
    public class ProgressMeterSetupSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.Update, 5, UpdateMasks.SetupMask),
                new SysPermissions()
                    .ReadWriteShared<ProgressMeterState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ProgressMeterState meterState,
                out PlayerProgressState progressState
                );

            if (meterState.CurrentDayIdx == -1)
            {
                ProgressMeterUtility.SetCurrentDay(meterState, progressState.ElapsedCycles);
            }
        }
    }
}

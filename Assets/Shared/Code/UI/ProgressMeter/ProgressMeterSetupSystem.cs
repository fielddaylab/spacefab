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
                ProgressMeterUtility.SetCurrentDay(meterState, progressState.ElapsedCycles - 1);

                // fill locked-in cycles up to current
                for (int i = 0; i < progressState.ElapsedCycles; i++)
                {
                    ProgressMeterUtility.SetCycleCellState(meterState, i, CycleCellState.FILLED);
                }

                // fill locked-in funds up to current
                for (int i = 0; i < progressState.Funds; i++)
                {
                    ProgressMeterUtility.SetFundsCellState(meterState, i, FundsCellState.FILLED);
                }
            }
        }
    }
}

using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab {
    /// <summary>
    /// Each LateUpdate, mirrors PlayerProgressState.ElapsedCycles into the marker index
    /// and pushes ProgressMeterState through to its bound view when the dirty flag is set.
    /// On first run with no bound view, performs a one-time scene scan to recover the
    /// reference (covers cases where the view enabled before this state registered).
    /// </summary>
    public class ProgressMeterUpdateSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhaseMask.LateUpdate, 0, UpdateMasks.OverarchingMask),
                new SysPermissions()
                    .ReadWriteShared<ProgressMeterState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        // Per-tick: ensure the view is bound, sync ElapsedCycles, drain the dirty flag.
        static private void ProcessWork(float deltaTime) {
            Find.State(
                out ProgressMeterState meterState,
                out PlayerProgressState progressState
                );

            // Drain the dirty flag by pushing state into the view.
            if (meterState.NeedsRefresh) {
                ProgressMeterUtility.RefreshVisuals(meterState.ActiveMeter, meterState);
                meterState.NeedsRefresh = false;
            }
        }
    }
}

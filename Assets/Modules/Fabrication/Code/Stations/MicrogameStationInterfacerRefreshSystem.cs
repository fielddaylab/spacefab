using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Fabrication.Stations {
    /// <summary>
    /// Clears MicrogameStationInterfacer's per-interfacer one-frame flags (CompletedThisFrame,
    /// ProcessAnimationStartedThisFrame) at end of frame, after MicrogameStationInterfacerBridgeSystem
    /// has forwarded them to StationControlState. Runs on LateUpdate at order 100 under AttemptMask.
    /// </summary>
    public class MicrogameStationInterfacerRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .ReadWrite<MicrogameStationInterfacer>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            var interfacers = Find.Components<MicrogameStationInterfacer>();
            for (int i = 0; i < interfacers.Count; i++) {
                interfacers[i].CompletedThisFrame = false;
                interfacers[i].ProcessAnimationStartedThisFrame = false;
            }
        }
    }
}

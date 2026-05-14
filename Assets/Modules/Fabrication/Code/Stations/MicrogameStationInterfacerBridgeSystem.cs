using FieldDay;
using FieldDay.Systems;
using SpaceFab.Fabrication.StationControl;

namespace SpaceFab.Fabrication.Stations {
    /// <summary>
    /// Forwards per-interfacer one-frame signal flags (CompletedThisFrame, ProcessAnimationStartedThisFrame)
    /// into StationControlState via the NotifyMicrogameCompleted / NotifyProcessAnimationStarted utilities.
    /// Decouples microgame implementations from StationControlState lookups — microgames mutate only their
    /// own interfacer, and this bridge enforces the cross-state coupling.
    ///
    /// Runs on Update at order 4 (immediately before StationControlSystem at order 5) so signals raised in
    /// the preceding FixedUpdate or earlier this frame are seen same-frame by the state machine.
    /// </summary>
    public class MicrogameStationInterfacerBridgeSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 4, UpdateMasks.AttemptMask),
                new SysPermissions()
                    .Read<MicrogameStationInterfacer>()
                    .ReadWriteShared<StationControlState>()
            );
        }

        // Scans every interfacer for raised one-frame flags and routes them to StationControlState.
        static private void ProcessWork(float deltaTime) {
            StationControlState stationState = Find.State<StationControlState>();
            var interfacers = Find.Components<MicrogameStationInterfacer>();
            for (int i = 0; i < interfacers.Count; i++) {
                MicrogameStationInterfacer interfacer = interfacers[i];
                if (interfacer.CompletedThisFrame) {
                    StationControlUtility.NotifyMicrogameCompleted(stationState);
                }
                if (interfacer.ProcessAnimationStartedThisFrame) {
                    StationControlUtility.NotifyProcessAnimationStarted(stationState);
                }
            }
        }
    }
}

using FieldDay;
using FieldDay.Systems;
using SpaceFab;

namespace SpaceFab.Research {
    /// <summary>
    /// Clears every per-frame field on ResearchUIInputState at end-of-frame
    /// so click handlers can re-arm them on the next tick. Runs after both
    /// the viewmodel systems (which read these flags) and the observation /
    /// submit systems (which consume them).
    /// </summary>
    public class ResearchUIInputRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 1000, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadWriteShared<ResearchUIInputState>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(out ResearchUIInputState inputState);
            ResearchUIInputUtility.ClearFrameFlags(inputState);
        }
    }
}

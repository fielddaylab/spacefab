using FieldDay;
using FieldDay.Systems;
using SpaceFab;

namespace SpaceFab.Research {
    /// <summary>
    /// Clears per-frame flags on ResearchMinigameState at end-of-frame
    /// so each one is only true during the frame in which it was raised.
    /// Today: PropertyConfirmedThisFrame, raised by
    /// ResearchPropertyConfirmBridge and consumed by
    /// ResearchSampleTrayRefreshSystem (and any future view that needs
    /// to react to a new sandbox confirmation).
    /// Runs on LateUpdate at order 1000 under ResearchMask, after every
    /// consumer that wants to read the flags this frame.
    /// </summary>
    public class ResearchMinigameStateRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 1000, UpdateMasks.ResearchMask),
                new SysPermissions().ReadWriteShared<ResearchMinigameState>());
        }

        private static void ProcessWork(float deltaTime) {
            ResearchMinigameState researchState = Find.State<ResearchMinigameState>();
            if (researchState == null) return;
            researchState.PropertyConfirmedThisFrame = false;
        }
    }
}

using FieldDay;
using FieldDay.Systems;
using SpaceFab;

namespace SpaceFab.Research {
    /// <summary>
    /// Re-applies visual properties to every tray sample source's rig
    /// whenever a property has been confirmed this frame. The rig's
    /// label flips from sample-number to ShortName the first time any
    /// property is confirmed for a material; this system is what
    /// catches that transition so tray gems update immediately on
    /// hypothesis confirmation.
    ///
    /// Gated on ResearchMinigameState.PropertyConfirmedThisFrame.
    /// ResearchMinigameStateRefreshSystem at LateUpdate 1000 clears
    /// the flag.
    ///
    /// Runs on LateUpdate at order 400 — after
    /// HypothesisViewModelSystem (100) so the sandbox is fully
    /// up-to-date for any consumer that reads it, and before the
    /// LateUpdate-500 visual systems that read the rig.
    /// </summary>
    public class ResearchSampleTrayRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 400, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchMinigameState>()
                    .ReadShared<ResearchSampleTrayState>()
                    .ReadWrite<ResearchMaterialSource>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchMinigameState researchState,
                out ResearchSampleTrayState trayState
            );

            if (researchState == null || !researchState.PropertyConfirmedThisFrame) return;
            if (trayState == null || trayState.SpawnedSamples == null) return;

            for (int i = 0; i < trayState.SpawnedSamples.Count; i++) {
                ResearchMaterialSource source = trayState.SpawnedSamples[i];
                if (source == null || source.Rig == null || source.Material == null) continue;
                ResearchMaterialVisualRigUtility.ApplyPropertiesToRig(source.Rig, source.Material, researchState);
            }
        }
    }
}

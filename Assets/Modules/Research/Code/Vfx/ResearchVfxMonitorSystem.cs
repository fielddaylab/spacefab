using FieldDay;
using FieldDay.Systems;
using FieldDay.UI;
using SpaceFab;

namespace SpaceFab.Research {
    /// <summary>
    /// Sweeps every active ResearchVfxInstance once per LateUpdate and frees
    /// it back to its pool when no particle systems and no animation routine
    /// are still running. Pool ownership is tracked by FieldDay's GuiCommands
    /// helper, so any instance allocated through ResearchVfxUtility.PlayFromPool
    /// is reclaimed here.
    /// </summary>
    public class ResearchVfxMonitorSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.UnscaledLateUpdate, 1000, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadWrite<ResearchVfxInstance>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            foreach (var instance in Find.Components<ResearchVfxInstance>()) {
                if (!ResearchVfxUtility.IsPlaying(instance)) {
                    GuiCommands.TryFreePrefab(instance);
                }
            }
        }
    }
}

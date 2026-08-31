using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.UI {
    /// <summary>
    /// Steps every registered SpriteCycler once per frame.
    ///
    /// Runs on LateUpdate at order 900 — behind the binders that swap a cycler's sequence during
    /// the frame (WikiRefreshSystem drains at 800), so a slot bound this frame starts cycling from
    /// the sequence it was just given rather than the previous one.
    ///
    /// Unmasked: cyclers are shared UI and can live in any scene, so gating this on any one
    /// minigame's mask would silently freeze the others.
    /// </summary>
    public class SpriteCycleSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 900),
                new SysPermissions()
                    .ReadWrite<SpriteCycler>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            var cyclers = Find.Components<SpriteCycler>();
            for (int i = 0; i < cyclers.Count; i++) {
                SpriteCyclerUtility.Advance(cyclers[i], deltaTime);
            }
        }
    }
}

using UnityEngine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;


namespace SpaceFab.UI {
    /// <summary>
    /// Authoring-level trigger for WikiPoolUtility.RebuildStrips. Runs on PostUpdate order 100
    /// under WikiMask, so any authoring-level changes to the content (unlock mutation, level
    /// load) are guaranteed to have been applied before it runs. RebuildStrips then releases all
    /// active buttons back to the free lists and acquires fresh ones according to the new
    /// content state.
    /// </summary>
    public class WikiSetupSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWrite<WikiButton>()
                    .Read<WikiContent>()
                    .ReadWrite<WikiPools>()
                    .ReadWriteShared<WikiState>()
                    .ReadWriteShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out WikiState wikiState,
                out PlayerProgressState progressState
                );

            var contents = Find.Components<WikiContent>();
            if (contents.Count == 0) { return; }
            WikiContent content = contents[0];

            var pools = Find.Components<WikiPools>();
            if (pools.Count == 0) { return; }
            WikiPoolUtility.RebuildStrips(content, pools[0]);
            WikiAvailabilityUtility.ApplyUnlocks(content, pools[0], progressState);
        }
    }
}
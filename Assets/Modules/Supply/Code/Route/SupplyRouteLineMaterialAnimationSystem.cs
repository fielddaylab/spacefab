using FieldDay;
using FieldDay.Rendering;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteLineMaterialAnimationSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ScrollLineMaterials, new SysUpdate(GameLoopPhase.UnscaledLateUpdate, 1000, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadWriteShared<SupplyRouteLineMaterialAnimationState>()
            );
        }

        static private void ScrollLineMaterials(float dt) {
            Find.State(out SupplyRouteLineMaterialAnimationState scrollState);
            scrollState.CurrentScroll = (scrollState.CurrentScroll + scrollState.ScrollSpeed * dt) % 1;

            Vector2 scrollVec = new Vector2(1 - scrollState.CurrentScroll, 0);

            for(int i = 0; i < scrollState.ScrollMaterials.Length; i++) {
                scrollState.ScrollMaterials[i].SetTextureOffset(DefaultShaderProps._MainTex, scrollVec);
            }
        }
    }
}
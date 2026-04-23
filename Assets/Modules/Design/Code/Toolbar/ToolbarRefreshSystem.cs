using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design {
    /// <summary>
    /// Clears the toolbar's one-frame input flags at end of frame. Runs on LateUpdate at
    /// order 100 under ToolModeMask so every earlier consumer (ToolbarSelectSystem on
    /// PreUpdate, ToolbarVisualsUpdateSystem on LateUpdate order 0) has seen the flags
    /// before they're wiped.
    /// </summary>
    public class ToolbarRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.ToolModeMask),
                new SysPermissions()
                    .ReadWrite<ToolbarButton>()
                    .ReadWriteShared<ToolbarState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            // Per-button flags — one-frame pointer events.
            var buttons = Find.Components<ToolbarButton>();
            for (int i = 0; i < buttons.Count; i++) {
                buttons[i].ClickedThisFrame = false;
                buttons[i].PointerEnterThisFrame = false;
                buttons[i].PointerExitThisFrame = false;
            }

            // State-level one-frame request.
            ToolbarState toolbarState = Find.State<ToolbarState>();
            toolbarState.ClearRequestedThisFrame = false;
        }
    }
}

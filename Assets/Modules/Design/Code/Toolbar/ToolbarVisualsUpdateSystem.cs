using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design {
    /// <summary>
    /// Drives the toolbar's presentation layer from ToolbarState + ToolModeState: row focus/
    /// fade, selection arrow position, and any "selected tool" text label.
    ///
    /// Runs on PreUpdate at order 10 under ToolModeMask — after ToolbarSelectSystem has
    /// finished mutating state and before ToolbarRefreshSystem (Update order 0) clears the
    /// one-frame flags.
    ///
    /// Body is currently stubbed. Fields and permissions are declared so the consuming scene
    /// prefab can be built against a stable shape.
    /// </summary>
    public class ToolbarVisualsUpdateSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 10, UpdateMasks.ToolModeMask),
                new SysPermissions()
                    .ReadShared<ToolbarState>()
                    .ReadShared<ToolModeState>()
                    .ReadWrite<ToolbarButton>()
                    .ReadWrite<ToolbarRow>()
            );
        }

        // TODO: implement visual refresh.
        //
        // Rough shape:
        //   ToolbarState toolbarState = Find.State<ToolbarState>();
        //   ToolModeState toolModeState = Find.State<ToolModeState>();
        //
        //   1. Row fade. Walk Find.Components<ToolbarRow>() and set each row's FadeGroup.alpha:
        //      full opacity if row.Row == toolbarState.FocusedRow, faded otherwise.
        //
        //   2. Selection arrow. Move or retarget the arrow to toolbarState.CurrentArrowAnchor.
        //      If null (no tool selected yet), hide the arrow.
        //
        //   3. Selected-tool text label. Look up a human-readable name for
        //      toolModeState.ActiveTool and update the label.
        //
        // All three should be idempotent — it's fine to re-run them every frame.
        static private void ProcessWork(float deltaTime) {
        }
    }
}

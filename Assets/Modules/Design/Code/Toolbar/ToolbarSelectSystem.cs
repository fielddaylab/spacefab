using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Design {
    /// <summary>
    /// Consumes per-button one-frame pointer flags and routes them into ToolbarState /
    /// ToolModeState. Runs on PreUpdate at order 0 under ToolModeMask, so
    /// ToolModeState.ActiveTool / ActiveLayer are already current by the time
    /// ToolInteractSystem (Update, order 10) reads them.
    ///
    /// Mirrors the three-pass exit/enter/click ordering from SelectMinigameZoneSystem — it
    /// prevents a same-frame "exit button A, enter button B" from leaking a stale hover
    /// override when B is in the same row as the selected tool.
    /// </summary>
    public class ToolbarSelectSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.PreUpdate, 0, UpdateMasks.ToolModeMask),
                new SysPermissions()
                    .ReadWrite<ToolbarButton>()
                    .ReadWriteShared<ToolbarState>()
                    .ReadWriteShared<ToolModeState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            ToolbarState toolbarState = Find.State<ToolbarState>();
            ToolModeState toolModeState = Find.State<ToolModeState>();
            var buttons = Find.Components<ToolbarButton>();

            // Pass 1: pointer-exit events. Clear any hover override first so a same-frame
            // exit + enter doesn't get reversed by the enter handler seeing a stale focused row.
            //
            // The Available guard protects against stale flags set during a level-load race
            // (a button could receive a pointer event the frame its GameObject is being
            // deactivated). Shouldn't happen in practice but the check is cheap.
            for (int i = 0; i < buttons.Count; i++) {
                if (!buttons[i].Available) { continue; }
                if (buttons[i].PointerExitThisFrame) {
                    ToolbarUtility.EndHover(toolbarState, toolModeState);
                }
            }

            // Pass 2: pointer-enter events. Swaps focus if the hovered row differs from the
            // selected tool's row. Within-row hovers are no-ops.
            for (int i = 0; i < buttons.Count; i++) {
                if (!buttons[i].Available) { continue; }
                if (buttons[i].PointerEnterThisFrame) {
                    ToolbarUtility.BeginHover(toolbarState, toolModeState, buttons[i].Row);
                }
            }

            // Pass 3: click events. Clear is the special case — one-shot request, no tool
            // selection. Everything else routes through SelectTool.
            for (int i = 0; i < buttons.Count; i++) {
                if (!buttons[i].Available) { continue; }
                if (!buttons[i].ClickedThisFrame) { continue; }

                if (buttons[i].Kind == ToolbarButtonKind.Clear) {
                    ToolbarUtility.RequestClear(toolbarState);
                }
                else {
                    ToolbarUtility.SelectTool(toolModeState, toolbarState, buttons[i].Kind, buttons[i].Row);
                    SpacefabGame.Events.Dispatch(GameEvents.DesignToolSelected, EvtArgs.Create(toolModeState.ActiveTool));
                    toolbarState.CurrentArrowAnchor = buttons[i].ArrowAnchor;
                }
            }
        }
    }
}

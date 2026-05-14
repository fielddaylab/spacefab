using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.UI {
    /// <summary>
    /// Clears per-button one-frame pointer flags at end of frame. Runs on Update order 0 under
    /// WikiMask so every earlier consumer (WikiSelectSystem on PreUpdate 0,
    /// WikiVisualsUpdateSystem on PreUpdate order 10) has seen the flags before they're wiped.
    ///
    /// The state-level external request flags (OpenRequestedThisFrame etc.) are cleared inline
    /// by WikiSelectSystem when consumed, so they are not handled here.
    /// </summary>
    public class WikiRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.WikiMask),
                new SysPermissions()
                    .ReadWrite<WikiButton>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            var buttons = Find.Components<WikiButton>();
            for (int i = 0; i < buttons.Count; i++) {
                buttons[i].ClickedThisFrame = false;
                buttons[i].PointerEnterThisFrame = false;
                buttons[i].PointerExitThisFrame = false;
            }
        }
    }
}

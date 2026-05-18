using FieldDay;
using FieldDay.HID;
using FieldDay.Systems;
using SpaceFab;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Dismisses the observation picker when the player presses outside
    /// it while it's open. Press position is hit-tested against the Add
    /// Observation button and the picker overlay's RectTransforms — a
    /// press inside either is left for that element's own onClick
    /// (mouse-up) handler to act on.
    ///
    /// Press-frame timing: Game.Input.IsMousePressed fires on the press
    /// frame, but CursorHint / PointerListener's onClick fires on
    /// mouse-up (one frame later). The geometric hit-test is necessary
    /// because the frame-flags those handlers raise are not yet set
    /// when this system runs.
    ///
    /// Runs on Update order 200 under ResearchMask, after
    /// ResearchDragSystem (100) so a click that triggers a drag deposit
    /// still gets a chance to close the picker on the same frame.
    /// </summary>
    public class ObservationPickerOffClickSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 200, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadShared<ResearchUIInputState>()
                    .ReadWrite<ResearchSamplePanel>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            if (!Game.Input.IsMousePressed(MouseButton.Left)) return;

            Vector2 mousePos = Input.mousePosition;
            Camera uiCamera = Game.Gui.PrimaryCamera;
            foreach (var panel in Find.Components<ResearchSamplePanel>()) {
                if (panel == null || !panel.PickerOpen) continue;

                // Skip if the press lands on the Add button — its own
                // toggle handler runs on mouse-up.
                if (panel.AddObservationButton != null) {
                    RectTransform buttonRect = panel.AddObservationButton.transform as RectTransform;
                    if (buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePos, uiCamera)) {
                        continue;
                    }
                }
                // Skip if the press lands anywhere on the picker
                // overlay (chips are sized to fit inside the overlay's
                // rect, so this covers chip clicks too).
                if (panel.ChipPickerOverlay != null) {
                    RectTransform overlayRect = panel.ChipPickerOverlay.transform as RectTransform;
                    if (overlayRect != null && RectTransformUtility.RectangleContainsScreenPoint(overlayRect, mousePos, uiCamera)) {
                        continue;
                    }
                }

                SamplePanelInputUtility.ClosePicker(panel);
            }
        }
    }
}

using FieldDay;
using FieldDay.HID;
using FieldDay.Physics;
using FieldDay.Systems;
using SpaceFab;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Polls input and drives the Research drag-and-drop state machine. On
    /// left-click, runs Physics2D overlap queries against the gem and slot
    /// layers and dispatches lift / deposit / cancel through ResearchDragUtility.
    /// On right-click, cancels any active drag. Each frame, repositions the
    /// drag preview to the cursor's world position.
    /// </summary>
    public class ResearchDragSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 100, UpdateMasks.ResearchMask),
                new SysPermissions()
                    .ReadWriteShared<ResearchDragState>()
                    .ReadWriteShared<ChamberInterfacerState>()
                    .ReadWrite<ResearchSlot>()
                    .ReadWrite<ResearchMaterialSource>()
            );
        }

        private static void ProcessWork(float deltaTime) {
            Find.State(
                out ResearchDragState dragState,
                out ChamberInterfacerState interfacerState
            );

            // 1. Resolve cursor position. Click handling bails when the cursor
            // is over UI canvas or outside the virtual viewport, but the drag
            // preview still tracks world position so it doesn't freeze.
            Camera cam = Game.Rendering.PrimaryCamera;
            Vector2 worldPos = Vector2.zero;
            bool worldValid = cam != null;
            if (worldValid) {
                Vector3 worldPos3 = cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos = new Vector2(worldPos3.x, worldPos3.y);
            }
            bool cursorOnCanvas = Game.Input.IsPointerOverCanvas();
            bool cursorValid = worldValid && CursorUtility.IsCursorWithinVirtualViewport() && !cursorOnCanvas;

            // 2. Right-click always cancels.
            bool cancelQueued = false;
            if (Game.Input.IsMousePressed(MouseButton.Right)) {
                cancelQueued = true;
            } else {
                bool leftClicked = Game.Input.IsMousePressed(MouseButton.Left);
                OverlapResults overlap = default;

                if (dragState.CurrentlyDragging != null) {
                    // 2a. Drag in progress — overlap query runs regardless of
                    // click so hover tracking still works.
                    if (cursorValid) {
                        GetPositionOverlap(worldPos, out overlap);
                    }

                    if (leftClicked) {
                        if (overlap.Slot != null) {
                            ResearchDragUtility.DepositCurrentDrag(dragState, interfacerState, overlap.Slot);
                        } else if (overlap.Gem != null) {
                            // Clicking the same material cancels; clicking a
                            // different gem swap-lifts it.
                            if (overlap.Gem.Material == dragState.CurrentlyDragging) {
                                cancelQueued = true;
                            } else {
                                ResearchDragUtility.LiftItem(dragState, interfacerState, overlap.Gem);
                            }
                        } else {
                            cancelQueued = true;
                        }
                    } else {
                        // Just track hover. TODO: hover VFX hookup once slots
                        // have a HoverVfx field.
                        dragState.SlotHoveredOver = overlap.Slot;
                    }
                } else if (cursorValid && leftClicked) {
                    // 2b. Idle, valid click — try to lift. Gem takes priority
                    // over slot if both overlap.
                    GetPositionOverlap(worldPos, out overlap);
                    if (overlap.Gem != null) {
                        ResearchDragUtility.LiftItem(dragState, interfacerState, overlap.Gem);
                    } else if (overlap.Slot != null && overlap.Slot.AllowLift && overlap.Slot.CurrentMaterial != null) {
                        ResearchDragUtility.LiftFromSlot(dragState, interfacerState, overlap.Slot);
                    }
                }
            }

            if (cancelQueued) {
                ResearchDragUtility.CancelCurrentDrag(dragState, interfacerState);
            }

            // 3. Reposition the drag preview to follow the cursor.
            if (worldValid && dragState.CurrentlyDragging != null && dragState.DragRenderer != null) {
                Transform t = dragState.DragRenderer.transform;
                Vector3 p = t.position;
                t.position = new Vector3(worldPos.x, worldPos.y, p.z);
            }
        }

        // Single-radius Physics2D overlap query against the gem and slot
        // layers, resolved to their respective components. Both fields are
        // null if no collider on the queried layer was at the cursor.
        private static void GetPositionOverlap(Vector2 worldPos, out OverlapResults results) {
            const float OverlapRadius = 0.01f;
            Collider2D gemCollider = Physics2D.OverlapCircle(worldPos, OverlapRadius, LayerMasks.ResearchGem_Mask);
            Collider2D slotCollider = Physics2D.OverlapCircle(worldPos, OverlapRadius, LayerMasks.ResearchSlot_Mask);
            results.Gem = gemCollider != null ? gemCollider.ResolveComponent<ResearchMaterialSource>() : null;
            results.Slot = slotCollider != null ? slotCollider.ResolveComponent<ResearchSlot>() : null;
        }

        private struct OverlapResults {
            public ResearchMaterialSource Gem;
            public ResearchSlot Slot;
        }
    }
}

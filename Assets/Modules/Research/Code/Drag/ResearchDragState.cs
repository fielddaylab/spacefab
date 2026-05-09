using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using SpaceFab;
using SpaceFab.Materials;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceFab.Research {
    /// <summary>
    /// Shared state for the Research drag-and-drop loop. Holds the cursor-
    /// following preview rig, cursor lock hint, raycaster reference, and the
    /// runtime drag payload (currently-dragged material, hovered slot, source
    /// slot for cancel-restore). All fields default to a not-dragging state;
    /// the drag system populates them on lift and clears them on drop/cancel.
    /// </summary>
    public class ResearchDragState : SharedStateComponent, IRegistrationCallbacks {
        public ResearchMaterialRig DragRenderer;
        public CursorHint DragCursor;
        // PhysicsRaycaster covers both itself and Physics2DRaycaster (subclass),
        // letting the scene's camera use whichever fits its physics layout.
        // The drag system reads/writes eventMask, declared on PhysicsRaycaster
        // (not on BaseRaycaster, and not on GraphicRaycaster — which uses
        // blockingMask instead and is intentionally unsupported here).
        public PhysicsRaycaster Raycaster;

        // Global swap gate, ANDed with ResearchSlot.AllowSwap. Lets the user
        // disable swapping mid-game (e.g., during a tutorial step) without
        // changing every slot.
        public bool AllowSwap = true;

        [NonSerialized] public MaterialAsset CurrentlyDragging;
        [NonSerialized] public ResearchSlot SlotHoveredOver;
        [NonSerialized] public ResearchSlot LiftedFromSlot;
        [NonSerialized] public ChamberSlotKind LiftedFromKind;

        public void OnRegister() {
            // Drag preview is hidden until the player lifts something.
            if (DragRenderer != null) {
                DragRenderer.gameObject.SetActive(false);
            }
        }

        public void OnDeregister() {
            CurrentlyDragging = null;
            SlotHoveredOver = null;
            LiftedFromSlot = null;
        }
    }

    /// <summary>
    /// Logic paired with ResearchDragState. Lift / Deposit / Cancel form the
    /// drag-state machine. ResearchDragSystem resolves both shared states once
    /// per ProcessWork and passes them in. Slot mutation routes through
    /// ResearchSlotUtility.FillInSlot, which raises the chamber frame-flag.
    /// </summary>
    public static class ResearchDragUtility {
        // Begins a drag from a free-floating source in the world. If the source
        // is bound to a slot (CurrentSlot != null), the slot is emptied and
        // remembered so a cancel can restore the material.
        public static bool LiftItem(ResearchDragState dragState, ChamberInterfacerState interfacerState, ResearchMaterialSource source) {
            if (source == null || source.Material == null) {
                return false;
            }
            return BeginDrag(dragState, interfacerState, source.Material, source.CurrentSlot);
        }

        // Begins a drag by lifting a filled slot's current material. Used when
        // the player clicks the slot directly rather than a free-floating gem.
        public static bool LiftFromSlot(ResearchDragState dragState, ChamberInterfacerState interfacerState, ResearchSlot slot) {
            if (slot == null || slot.CurrentMaterial == null) {
                return false;
            }
            return BeginDrag(dragState, interfacerState, slot.CurrentMaterial, slot);
        }

        // Drops the currently-dragged material into the given slot. If the slot
        // already holds a material and both global and per-slot AllowSwap are
        // set, swaps: the slot's material becomes the new drag payload and the
        // drag continues. Otherwise ends the drag. Returns true if a fill or
        // swap was applied.
        public static bool DepositCurrentDrag(ResearchDragState dragState, ChamberInterfacerState interfacerState, ResearchSlot slot) {
            if (dragState.CurrentlyDragging == null) {
                return false;
            }

            // Reject slots that aren't wired as Primary or Secondary.
            ChamberSlotKind? kindOpt = ChamberInterfacerUtility.KindOf(interfacerState, slot);
            if (!kindOpt.HasValue) {
                CancelCurrentDrag(dragState, interfacerState);
                return false;
            }
            ChamberSlotKind kind = kindOpt.Value;

            // 1. A swap requires the slot to be filled with a different
            // material and both AllowSwap gates set.
            MaterialAsset swap = null;
            if (dragState.AllowSwap && slot.AllowSwap && slot.CurrentMaterial != null && slot.CurrentMaterial != dragState.CurrentlyDragging) {
                swap = slot.CurrentMaterial;
            }

            // 2. Fill the destination. A non-receptive slot causes FillInSlot
            // to return false, in which case we cancel.
            MaterialAsset toFill = dragState.CurrentlyDragging;
            if (!ResearchSlotUtility.FillInSlot(interfacerState, slot, kind, toFill)) {
                CancelCurrentDrag(dragState, interfacerState);
                return false;
            }

            if (swap != null) {
                // 3a. Swap: drag picks up the slot's previous occupant.
                // LiftedFromSlot becomes this slot so a later cancel restores
                // the swapped material here.
                dragState.CurrentlyDragging = swap;
                dragState.LiftedFromSlot = slot;
                dragState.LiftedFromKind = kind;
                ResearchMaterialRigUtility.ApplyPropertiesToRig(dragState.DragRenderer, swap);
            } else {
                // 3b. No swap: end the drag.
                EndDrag(dragState);
            }
            return true;
        }

        // Aborts the drag. If the source was a slot, the original material is
        // returned to it. If the source was free-floating, the material is
        // dropped (no restore target).
        public static bool CancelCurrentDrag(ResearchDragState dragState, ChamberInterfacerState interfacerState) {
            if (dragState.CurrentlyDragging == null) {
                return false;
            }

            // If the source slot's receptive flag has been flipped off
            // mid-drag, FillInSlot will reject the restore and the material is
            // lost. That requires a chamber to have actively deactivated the
            // source slot, which is a legitimate game-state outcome.
            if (dragState.LiftedFromSlot != null) {
                ResearchSlotUtility.FillInSlot(interfacerState, dragState.LiftedFromSlot, dragState.LiftedFromKind, dragState.CurrentlyDragging);
            }

            EndDrag(dragState);
            return true;
        }

        // Shared lift entry: stores the payload, captures the source slot if
        // any, shows the drag preview, locks the cursor, and strips UI_Mask
        // from the raycaster so the preview doesn't intercept its own hovers.
        private static bool BeginDrag(ResearchDragState dragState, ChamberInterfacerState interfacerState, MaterialAsset material, ResearchSlot sourceSlot) {
            dragState.CurrentlyDragging = material;
            dragState.LiftedFromSlot = sourceSlot;
            dragState.LiftedFromKind = ChamberSlotKind.Primary;

            // If the source is a slot, clear it. Resolving the kind first so
            // a later cancel knows which slot to restore to.
            if (sourceSlot != null) {
                ChamberSlotKind? kindOpt = ChamberInterfacerUtility.KindOf(interfacerState, sourceSlot);
                if (kindOpt.HasValue) {
                    dragState.LiftedFromKind = kindOpt.Value;
                    ResearchSlotUtility.FillInSlot(interfacerState, sourceSlot, kindOpt.Value, null);
                } else {
                    // Source slot isn't wired to the active chamber; treat as
                    // free-floating so cancel doesn't try to restore.
                    dragState.LiftedFromSlot = null;
                }
            }

            if (dragState.DragRenderer != null) {
                dragState.DragRenderer.gameObject.SetActive(true);
                ResearchMaterialRigUtility.ApplyPropertiesToRig(dragState.DragRenderer, material);
            }
            CursorHint.TryLock(dragState.DragCursor);
            if (dragState.Raycaster != null) {
                dragState.Raycaster.eventMask &= ~LayerMasks.UI_Mask;
            }
            return true;
        }

        // Shared drag-end teardown: hides the preview, unlocks the cursor,
        // restores the raycaster's UI_Mask, and clears runtime fields.
        private static void EndDrag(ResearchDragState dragState) {
            dragState.CurrentlyDragging = null;
            dragState.LiftedFromSlot = null;
            if (dragState.DragRenderer != null) {
                dragState.DragRenderer.gameObject.SetActive(false);
            }
            CursorHint.Unlock(dragState.DragCursor);
            if (dragState.Raycaster != null) {
                dragState.Raycaster.eventMask |= LayerMasks.UI_Mask;
            }
            dragState.SlotHoveredOver = null;
        }
    }
}

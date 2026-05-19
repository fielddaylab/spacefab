using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// A single material slot in the Research minigame. Holds zero or one
    /// MaterialAsset and renders the held material via its own visual rig.
    /// The slot does not know whether it is Primary or Secondary, nor whether
    /// it is currently receptive to writes — that classification lives on
    /// ChamberInterfacerState and is owned by chamber-side systems.
    /// </summary>
    public class ResearchSlot : BatchedComponent, IRegistrationCallbacks {
        public Collider2D Region;
        public Transform Root;
        public ResearchMaterialVisualRig Rig;

        // Drag-drop policy flags. AllowLift gates clicking the slot itself to
        // pull its current material back into a drag. AllowSwap gates dropping
        // a dragged material onto a filled slot, which atomically swaps the
        // held material with the dragged one. Both default false: an opt-in
        // slot is the safer default since enabling either changes the slot's
        // interactable surface.
        public bool AllowLift;
        public bool AllowSwap;

        [NonSerialized] public MaterialAsset CurrentMaterial;

        // Handle to the slot's in-flight explosion routine, when the held
        // material has been triggered to explode (e.g. voltage breakdown).
        // Owned by ResearchExplosionUtility; ResearchExplosionSystem reads
        // its truthiness to know when the global explosion lock can lift.
        [NonSerialized] public Routine ExplosionRoutine;

        public void OnRegister() {
        }

        public void OnDeregister() {
            CurrentMaterial = null;
            ExplosionRoutine.Stop();
        }
    }

    /// <summary>
    /// Logic for ResearchSlot. FillInSlot is the single insertion API; it gates
    /// writes through ChamberInterfacerState's per-slot receptive flag, updates
    /// the slot's visual rig, and raises the shared-state frame-flag so chamber
    /// systems can react.
    /// </summary>
    public static class ResearchSlotUtility {
        // Sets or clears the slot's held material. Returns true if the write
        // was applied; returns false (no-op) if the slot kind is currently
        // marked non-receptive. Writing the same material a slot already holds
        // is still treated as a write so the frame-flag fires.
        public static bool FillInSlot(ChamberInterfacerState interfacerState, ResearchSlot slot, ChamberSlotKind kind, MaterialAsset material) {
            if (!ChamberInterfacerUtility.IsReceptive(interfacerState, kind)) {
                return false;
            }

            slot.CurrentMaterial = material;
            if (slot.Rig != null) {
                if (material == null) {
                    ResearchMaterialVisualRigUtility.ClearRig(slot.Rig);
                } else {
                    // The slot rig's label is hidden in the chamber
                    // prefab — pass null for researchState since the
                    // known/unknown distinction only affects the label.
                    ResearchMaterialVisualRigUtility.ApplyPropertiesToRig(slot.Rig, material, null);
                }
            }

            ChamberInterfacerUtility.MarkSlotUpdated(interfacerState, kind, material);
            return true;
        }
    }
}

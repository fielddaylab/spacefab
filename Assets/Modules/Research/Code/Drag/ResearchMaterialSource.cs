using FieldDay;
using FieldDay.Components;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// A free-floating draggable material gem in the Research minigame. Any
    /// GameObject that should be pickable as a drag source carries one of these
    /// (e.g., tray slots, debug spawners). The drag system overlap-tests its
    /// Region collider against ResearchGem_Mask and lifts on click.
    /// CurrentSlot is null for free-floating draggables; if non-null, the drag
    /// system treats the lift as originating from that slot and clears it
    /// during pickup so the drag can be cancelled-back-to-source cleanly.
    /// </summary>
    public class ResearchMaterialSource : BatchedComponent, IRegistrationCallbacks {
        public Collider2D Region;

        public MaterialAsset Material;
        [NonSerialized] public ResearchSlot CurrentSlot;

        public void OnRegister() {
        }

        public void OnDeregister() {
            Material = null;
            CurrentSlot = null;
        }
    }
}

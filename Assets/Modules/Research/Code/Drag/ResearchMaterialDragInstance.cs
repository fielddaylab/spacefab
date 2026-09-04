using FieldDay;
using FieldDay.Components;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// A transient draggable material gem. Exists only while the player is
    /// actively dragging: allocated from ResearchMaterialInstancePool when a
    /// drag begins (from a Source on the tray or from a filled slot), released
    /// to the pool when the drag ends (dropped in a slot, dropped on a source,
    /// or cancelled). Carries the dragged MaterialAsset and a reference to
    /// the originating Source, if any, so cancel-paths can choose between
    /// "restore to slot" and "release without restore."
    /// </summary>
    public class ResearchMaterialDragInstance : BatchedComponent, IRegistrationCallbacks {
        public Collider2D Region;
        public ResearchMaterialVisualRig Rig;
        public MaterialAtom AtomicView;
        public MaterialPolyelementalAtom PolyelementalAtomicView;

        [NonSerialized] public MaterialAsset Material;
        [NonSerialized] public ResearchMaterialSource OriginSource;

        public void OnRegister() {
        }

        public void OnDeregister() {
            Material = null;
            OriginSource = null;
        }
    }
}

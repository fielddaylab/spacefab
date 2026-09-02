using FieldDay;
using FieldDay.Components;
using SpaceFab.Materials;
using SpaceFab.Onboarding;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// A permanent material fixture on the tray. One Source per material
    /// available in the chapter; clicking a Source allocates a transient
    /// ResearchMaterialInstance from the pool for the player to drag.
    /// The Source itself stays visible on the tray throughout — it is the
    /// well from which Instances are drawn, not a draggable in its own right.
    /// </summary>
    public class ResearchMaterialSource : BatchedComponent, IRegistrationCallbacks {
        public Collider2D Region;
        public ResearchMaterialVisualRig Rig;
        public Transform AtomicView;
        public MaterialAsset Material;

        // Onboarding tag stamped per spawn by ResearchSampleTrayUtility (id derived from
        // the material's DisplayName, e.g. "research:sample-copper"). Pre-wired on the prefab
        // with Collider already assigned — runtime only writes its Id via ElementTag.SetId.
        public ElementTag Tag;

        public void OnRegister() {
            // Renders any Material assigned in the inspector. Tray-spawned
            // sources also get an explicit apply after Material is assigned;
            // this covers scene-authored sources whose Material is already
            // set. researchState may be null pre-registration; the rig
            // falls back to the sample-number label and the tray refresh
            // system corrects it once a property is confirmed.
            if (Rig != null && Material != null) {
                ResearchMaterialVisualRigUtility.ApplyPropertiesToRig(Rig, Material, Find.State<ResearchMinigameState>());
            }
        }

        public void OnDeregister() {
            Material = null;
        }
    }
}

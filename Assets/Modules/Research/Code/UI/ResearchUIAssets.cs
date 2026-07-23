using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Global asset holding shared Research-UI visual values that are
    /// not chip-specific (chip sprites live on
    /// ResearchObservationChipAssets). Empty placeholder today — the
    /// previous tenants (pagination-dot tint colors) were retired when
    /// the paginator moved to sprite-layered dots. Add new shared UI
    /// tunables here as they come up.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Research/UI Assets")]
    public class ResearchUIAssets : GlobalAsset {
        // TODO: Replace chamber button sprites to the shared ButtonUp/ButtonDown sprites
        // with separate icon overlays to reduce the number of sprite assets.
        [Header("Chamber Button Sprites")]
        public Sprite VoltageNormal;
        public Sprite VoltagePressed;
        public Sprite ThermalNormal;
        public Sprite ThermalPressed;
        public Sprite DopingNormal;
        public Sprite DopingPressed;

        [Header ("Atomic View Toggle")]
        public Sprite ButtonUp;
        public Sprite ButtonDown;
    }
}

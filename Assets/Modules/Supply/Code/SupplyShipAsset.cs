using FieldDay;
using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.Supply {
    [CreateAssetMenu(menuName = "SpaceFab/Supply/Ship Asset")]
    public sealed class SupplyShipAsset : NamedAsset {
        public Sprite Icon;
        public ColorPalette2 Colors;
        public Color32 LineColor;

        [Header("Stats")]
        [Range(0, 2)] public int Speed;
        [Range(1, 3)] public int Capacity;
    }
}
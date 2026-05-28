using FieldDay.Assets;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    [CreateAssetMenu(menuName = "SpaceFab/Supply/Route Config")]
    public sealed class SupplyRouteConfig : GlobalAsset {
        [Header("Line Appearance")]
        public SupplyRouteLineConfig FragmentLine;
        public SupplyRouteLineConfig PendingFragmentLine;
        public SupplyRouteLineConfig EmptyCursorLine;
        public SupplyRouteLineConfig PendingCursorLine;
        public SupplyRouteLineConfig InvalidCursorLine;
        public SupplyRouteLineConfig ReturnLine;
        public SupplyRouteLineConfig SolidLine;

        [Header("Math Settings")]
        public float[] ShipSpeeds;
    }

    [Serializable]
    public struct SupplyRouteLineConfig {
        public float Width;
        public Material Material;
    }
}
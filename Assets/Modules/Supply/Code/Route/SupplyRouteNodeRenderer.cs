using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.UI;
using ScriptableBake;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteNodeRenderer : BatchedComponent, IBaked {
        [Header("Hover Objects")]
        public ActiveGroup HoverState;

#if UNITY_EDITOR
        int IBaked.Order { get { return 100; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            HoverState.SetActive(false);
            return true;
        }
#endif // UNITY_EDITOR
    }

    static public partial class SupplyRouteUtility {
        static public void SetHovering(SupplyRouteNodeRenderer renderer, bool hovering) {
            renderer.HoverState.SetActive(hovering);
        }
    }
}
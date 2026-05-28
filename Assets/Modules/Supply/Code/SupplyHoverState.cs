using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyHoverState : SharedStateComponent {
        public SupplyRouteNodeDetailsDisplay DetailsDisplay;

        [NonSerialized] public SupplyRouteNode Node;
        [NonSerialized] public EdgeCollider2D Route;
        [NonSerialized] public bool HoverDirty = false;

        [NonSerialized] public Vector3? MousePosition;
    }
}
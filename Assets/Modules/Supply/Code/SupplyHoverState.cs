using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyHoverState : SharedStateComponent {
        [NonSerialized] public SupplyRouteNode Node;
        [NonSerialized] public SupplyRouteRenderer Line;

        [NonSerialized] public Vector3? MousePosition;
    }
}
using BeauPools;
using FieldDay.Components;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteRenderer : BatchedComponent {
        [Header("Components")]
        public LineRenderer StaticLine;
        public LineRenderer ReturnLine;
    }
}
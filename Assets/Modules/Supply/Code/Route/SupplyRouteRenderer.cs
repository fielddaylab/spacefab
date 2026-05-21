using BeauPools;
using FieldDay.Components;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteRenderer : BatchedComponent {
        [Header("Components")]
        public EdgeCollider2D Collider;
        public LineRenderer StaticLine;
        public LineRenderer ReturnLine;
        public LineRenderer CursorLine;
    }
}
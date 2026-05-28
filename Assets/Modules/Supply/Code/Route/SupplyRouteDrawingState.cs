using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteDrawingState : SharedStateComponent, IRegistrationCallbacks {
        public LineRenderer CursorLine;
        public LineRenderer PreviewDeleteLine;
        public EdgeCollider2D RouteCollider;

        [NonSerialized] public SupplyRouteDrawPhase Phase;
        [NonSerialized] public int RouteIndex = -1;

        [NonSerialized] public SupplyRouteData PreviewRouteData;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            PreviewRouteData.Create();
        }
    }

    public enum SupplyRouteDrawPhase {
        Unselected,
        Drawing,
        Previewing
    }

    static public partial class SupplyRouteUtility {
        static public void UpdateRouteCollider(EdgeCollider2D collider, in SupplyRouteData routeData) {
            if (routeData.NodeCount < 2) {
                collider.enabled = false;
                return;
            }

            using(PooledList<Vector2> points = PooledList<Vector2>.Create()) {
                for(int i = 0; i < routeData.NodeCount; i++) {
                    points.Add(routeData.Nodes[i].transform.localPosition);
                }
                collider.SetPoints(points);
            }

            collider.enabled = true;
        }
    }
}
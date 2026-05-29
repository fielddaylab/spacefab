using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Mathematics;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteDrawingState : SharedStateComponent {
        public LineRenderer CursorLine;
        public LineRenderer PreviewDeleteLine;
        public EdgeCollider2D RouteCollider;

        [NonSerialized] public SupplyRouteDrawPhase Phase;
        [NonSerialized] public int RouteIndex = -1;

        [NonSerialized] public SupplyRouteDrawAction HoverAction;
        [NonSerialized] public int HoverActionArg;
        [NonSerialized] public bool PreviewDirty;
    }

    public enum SupplyRouteDrawPhase {
        Unselected,
        Drawing
    }

    public enum SupplyRouteDrawAction {
        None,

        AddNonTerminalNode,
        RemoveLastNode,
        RemoveSegment,

        CompleteRouteHome,
        CompleteRouteAuto,
        DeleteRoute,
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
    
        static public int TryGetClosestSegment(EdgeCollider2D collider, Vector2 position) {
            int closestSeg = -1;
            float closestDist = float.MaxValue;

            using(PooledList<Vector2> points = PooledList<Vector2>.Create()) {
                int segmentCount = collider.GetPoints(points) - 1;
                for(int i = 0; i < segmentCount; i++) {
                    float lenSq = LineMath.DistanceFromPointToLineSegmentSquared(position, points[i], points[i + 1]);
                    if (lenSq < closestDist) {
                        lenSq = closestDist;
                        closestSeg = i;
                    }
                }
            }

            return closestSeg;
        }

        static public bool SetDrawingHoverAction(SupplyRouteDrawingState draw, SupplyRouteDrawAction action, int actionArg) {
            if (draw.HoverAction != action || draw.HoverActionArg != actionArg) {
                draw.HoverAction = action;
                draw.HoverActionArg = actionArg;
                draw.PreviewDirty = true;
                return true;
            }

            return false;
        }
    }
}
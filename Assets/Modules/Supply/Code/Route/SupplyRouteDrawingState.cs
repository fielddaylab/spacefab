using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Mathematics;
using FieldDay.SharedState;
using FieldDay.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteDrawingState : SharedStateComponent {
        public LineRenderer CursorLine;
        public LineRenderer PreviewDeleteLine;
        public EdgeCollider2D RouteCollider;
        public PhysicsInputLayer InputLayer;

        [NonSerialized] public int RouteIndex = -1;
        [NonSerialized] public int QueuedRouteIndex = -1;
        [NonSerialized] public bool ForceUpdatePreview = false;

        [NonSerialized] public SupplyRouteDrawAction HoverAction;
        [NonSerialized] public int HoverActionArg;
        [NonSerialized] public bool PreviewDirty;
    }

    public enum SupplyRouteDrawAction {
        None,

        AddNonTerminalNode,
        RemoveLastNode,
        RemoveSegment,

        CompleteRouteHome,
        CompleteRouteAuto,
        DeleteRoute,
        DeleteRouteAuto,
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
                        closestDist = lenSq;
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

                Log.Msg("[SupplyRouteUtility] Set hover action to {0} with arg {1}", action, actionArg);
                return true;
            }

            return false;
        }
    
        static public void QueueRouteDrawing(int routeIndex) {
            Find.State(out SupplyRouteDrawingState draw);
            draw.QueuedRouteIndex = routeIndex;
        }

        static public void QueueRouteDrawingClose() {
            Find.State(out SupplyRouteDrawingState draw);
            draw.QueuedRouteIndex = -1;
        }

        static public void CloseRouteDrawing(SupplyRouteDrawingState draw, SupplyRouteCollection routes, SupplyShipIndex ships) {
            if (draw.RouteIndex < 0) {
                return;
            }

            routes.TempRouteBuffer.Clear();
            
            if (routes.TempRouteFragmentConsume >= 0) {
                routes.TempRouteFragmentConsume = -1;
                routes.AreFragmentsDirty = true;
            }
            if (routes.TempRouteFragmentCreate.NodeCount > 0) {
                routes.TempRouteFragmentCreate = default;
                routes.AreFragmentsDirty = true;
            }
            if (routes.Fragments.Count > 0) {
                routes.Fragments.Clear();
                routes.AreFragmentsDirty = true;
            }

            ref SupplyRouteData routeData = ref routes.Routes[draw.RouteIndex];
            ref SupplyRouteStats stats = ref routes.RouteStats[draw.RouteIndex];
            
            if (routeData.NodeCount < 2) {
                Log.Msg("[SupplyRouteUtility] route had too few nodes - killing");
                routeData.Clear();
                stats = default;
                routes.UpdatedRouteMask.Set(draw.RouteIndex);
            } else if (routeData.Nodes[0] != routeData.Nodes[routeData.NodeCount - 1]) {
                routeData.Flags |= SupplyRouteFlags.AutoConnectEnd;
                TryEvaluatePath(routeData, ships.ShipStats[draw.RouteIndex], draw.RouteIndex, out stats);
                routes.UpdatedRouteMask.Set(draw.RouteIndex);
            }

            draw.RouteCollider.enabled = false;
            draw.RouteIndex = -1;
            draw.ForceUpdatePreview = true;
        }

        static public void OpenRouteDrawing(SupplyRouteDrawingState draw, SupplyRouteCollection routes, SupplyShipIndex ships, int routeIndex) {
            Assert.True(routeIndex >= 0 && routeIndex < ships.ShipCount);
            if (draw.RouteIndex == routeIndex) {
                return;
            }

            CloseRouteDrawing(draw, routes, ships);

            draw.RouteIndex = routeIndex;
            ref SupplyRouteData routeData = ref routes.Routes[draw.RouteIndex];
            ref SupplyRouteStats stats = ref routes.RouteStats[draw.RouteIndex];

            Find.State(out SupplyChainMap map);

            routeData.Flags &= ~SupplyRouteFlags.AutoConnectEnd;

            if (routeData.NodeCount == 0) {
                routeData.Nodes[routeData.NodeCount++] = map.Home;
                routeData.NodeMask.Set(map.Home.Index);
                TryEvaluatePath(routeData, ships.ShipStats[routeIndex], routeIndex, out stats);
                routes.UpdatedRouteMask.Set(draw.RouteIndex);
            }

            UpdateRouteCollider(draw.RouteCollider, routeData);

            draw.ForceUpdatePreview = true;
        }
    }
}
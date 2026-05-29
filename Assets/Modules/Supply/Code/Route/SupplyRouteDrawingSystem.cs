using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteDrawingSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&DetermineRequestedActionSystem, new SysUpdate(GameLoopPhase.LateUpdate, -8, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadShared<SupplyRouteCollection>()
                    .ReadShared<SupplyHoverState>()
                    .ReadWriteShared<SupplyRouteDrawingState>());

            ecs.Register(&EvaluateActionPreview, new SysUpdate(GameLoopPhase.LateUpdate, -7, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadWriteShared<SupplyRouteCollection>()
                    .ReadShared<SupplyHoverState>()
                    .ReadWriteShared<SupplyRouteDrawingState>()
                    .ReadShared<SupplyShipIndex>());
        }

        // Determines the requested action based on what the player is currently hovering over
        static private void DetermineRequestedActionSystem(float dt) {
            Find.State(out SupplyRouteCollection routes, out SupplyHoverState hover, out SupplyRouteDrawingState draw);

            draw.PreviewDirty = false;

            if (draw.RouteIndex < 0 || draw.Phase == SupplyRouteDrawPhase.Unselected) {
                SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.None, 0);
                return;
            }

            ref SupplyRouteData routeData = ref routes.Routes[draw.RouteIndex];

            if (hover.HoverDirty || hover.Route != null) {
                if (hover.Route != null) {
                    int segment = SupplyRouteUtility.TryGetClosestSegment(hover.Route, hover.MousePosition.Value);
                    Assert.True(segment >= 0);
                    SupplyRouteUtility.SetDrawingHoverAction(draw, segment == 0 ? SupplyRouteDrawAction.DeleteRoute : SupplyRouteDrawAction.RemoveSegment, segment);
                } else if (hover.Node != null) {
                    if (hover.Node.Type == SupplyRouteNodeType.Home) {
                        if (routeData.NodeCount < 2) {
                            SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.DeleteRoute, 0);
                        } else {
                            SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.CompleteRouteHome, 0);
                        }
                    } else if (routeData.NodeCount > 1) {
                        if (SupplyRouteUtility.IsNodeInRoute(routeData, hover.Node)) {
                            if (routeData.Nodes[routeData.NodeCount - 1] == hover.Node) {
                                SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.RemoveLastNode, 0);
                            } else {
                                SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.None, 0);
                            }
                        } else {
                            SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.AddNonTerminalNode, 0);
                        }
                    }
                } else if (hover.MousePosition.HasValue) {
                    SupplyRouteUtility.SetDrawingHoverAction(draw, routeData.NodeCount > 2 ? SupplyRouteDrawAction.DeleteRoute : SupplyRouteDrawAction.CompleteRouteAuto, 0);
                } else {
                    SupplyRouteUtility.SetDrawingHoverAction(draw, SupplyRouteDrawAction.None, 0);
                }
            }
        }
    
        // Regenerates preview data for the player's action
        static private unsafe void EvaluateActionPreview(float dt) {
            Find.State(out SupplyRouteCollection routes, out SupplyHoverState hover, out SupplyRouteDrawingState draw, out SupplyShipIndex ships);

            if (!draw.PreviewDirty) {
                return;
            }

            ref SupplyRouteData previewRoute = ref routes.TempRouteBuffer;
            previewRoute.Clear();
            ref SupplyRouteStats previewStats = ref routes.TempRouteStats;
            previewStats = default;

            routes.TempRouteFragmentConsume = -1;
            routes.TempRouteFragmentCreate = default;

            if (draw.RouteIndex < 0 || draw.Phase == SupplyRouteDrawPhase.Unselected) {
                return;
            }

            SupplyShipStats shipStats = ships.ShipStats[draw.RouteIndex];

            SupplyRouteData currentRoute = routes.Routes[draw.RouteIndex];
            SupplyRouteStats currentStats = routes.RouteStats[draw.RouteIndex];

            SupplyRouteNode hoverNode = hover.Node;

            switch (draw.HoverAction) {
                case SupplyRouteDrawAction.None: {
                    previewStats = currentStats;
                    break;
                }

                case SupplyRouteDrawAction.AddNonTerminalNode: {
                    FragmentFindResult fragmentFind;
                    SupplyRouteData.Copy(currentRoute, ref previewRoute);
                    if (currentRoute.NodeCount >= SupplyRouteData.MaxNonTerminalNodes) {
                        previewStats = currentStats;
                        previewStats.Flags |= SupplyRouteResultFlags.PathTooLong;
                    } else if (SupplyRouteUtility.IsNodeInOtherRoutes(hoverNode, draw.RouteIndex, out int overlapIndex)) {
                        previewStats = currentStats;
                        previewStats.Flags |= SupplyRouteResultFlags.NodeInAnotherPath;
                    } else if ((fragmentFind = SupplyRouteUtility.TryFindFragment(hoverNode, out int fragmentIndex)) != FragmentFindResult.None) {
                        if (fragmentFind == FragmentFindResult.Middle) {
                            previewStats = currentStats;
                            previewStats.Flags |= SupplyRouteResultFlags.NodeInInvalidSegment;
                        } else {
                            SupplyRouteFragmentData fragmentData = routes.Fragments[fragmentIndex];
                            if (previewRoute.NodeCount + fragmentData.NodeCount > SupplyRouteData.MaxNonTerminalNodes) {
                                previewStats = currentStats;
                                previewStats.Flags |= SupplyRouteResultFlags.PathTooLong;
                            } else {
                                routes.TempRouteFragmentConsume = fragmentIndex;
                                if (fragmentFind == FragmentFindResult.Last) {
                                    for (int i = fragmentData.NodeCount; i-- > 0;) {
                                        int nodeIndex = fragmentData.Nodes[i];
                                        previewRoute.Nodes[previewRoute.NodeCount++] = SupplyRouteUtility.GetNodeForIndex(nodeIndex);
                                    }
                                } else {
                                    for (int i = 0; i < fragmentData.NodeCount; i++) {
                                        int nodeIndex = fragmentData.Nodes[i];
                                        previewRoute.Nodes[previewRoute.NodeCount++] = SupplyRouteUtility.GetNodeForIndex(nodeIndex);
                                    }
                                }
                                SupplyRouteUtility.TryEvaluatePath(previewRoute, shipStats, draw.RouteIndex, out previewStats);
                            }
                        }
                    } else {
                        previewRoute.Nodes[previewRoute.NodeCount++] = hoverNode;
                        SupplyRouteUtility.TryEvaluatePath(previewRoute, shipStats, draw.RouteIndex, out previewStats);
                    }
                    break;
                }

                case SupplyRouteDrawAction.RemoveSegment: {
                    int cutNodeIndex = draw.HoverActionArg + 1; // second node in segment
                    previewRoute.NodeCount = cutNodeIndex;
                    for(int i = 0; i < cutNodeIndex; i++) {
                        SupplyRouteNode node = currentRoute.Nodes[i];
                        previewRoute.Nodes[i] = node;
                        previewRoute.NodeMask.Set(node.Index);
                    }

                    int isolatedNodeCount = currentRoute.NodeCount - cutNodeIndex;
                    if (isolatedNodeCount > 1) {
                        SupplyRouteFragmentData fragment;
                        fragment.NodeCount = (byte) isolatedNodeCount;
                        for(int i = 0; i < isolatedNodeCount; i++) {
                            fragment.Nodes[i] = (byte)currentRoute.Nodes[cutNodeIndex + i].Index;
                        }
                        routes.TempRouteFragmentCreate = fragment;
                    }
                    SupplyRouteUtility.TryEvaluatePath(previewRoute, shipStats, draw.RouteIndex, out previewStats);
                    break;
                }

                case SupplyRouteDrawAction.RemoveLastNode: {
                    SupplyRouteData.Copy(currentRoute, ref previewRoute);
                    previewRoute.Nodes[--previewRoute.NodeCount] = null;
                    SupplyRouteUtility.TryEvaluatePath(previewRoute, shipStats, draw.RouteIndex, out previewStats);
                    break;
                }

                case SupplyRouteDrawAction.CompleteRouteAuto: {
                    SupplyRouteData.Copy(currentRoute, ref previewRoute);
                    previewRoute.Flags |= SupplyRouteFlags.AutoConnectEnd;
                    SupplyRouteUtility.TryEvaluatePath(previewRoute, shipStats, draw.RouteIndex, out previewStats);
                    break;
                }

                case SupplyRouteDrawAction.CompleteRouteHome: {
                    SupplyRouteData.Copy(currentRoute, ref previewRoute);
                    previewRoute.Nodes[previewRoute.NodeCount++] = hoverNode;
                    SupplyRouteUtility.TryEvaluatePath(previewRoute, shipStats, draw.RouteIndex, out previewStats);
                    break;
                }

                case SupplyRouteDrawAction.DeleteRoute: {
                    previewStats = default;
                    break;
                }
            }
        }
    }
}
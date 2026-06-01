using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteRenderSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&UpdateRouteRenderers, new SysUpdate(GameLoopPhase.LateUpdate, 2000, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadShared<SupplyRouteCollection>()
                    .ReadShared<SupplyRouteDrawingState>()
                    .ReadWriteShared<SupplyRouteRenderCollection>()
                    .ReadWriteShared<SupplyRoutePools>());

            ecs.Register(&UpdateFragmentPreviews, new SysUpdate(GameLoopPhase.LateUpdate, 2001, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadShared<SupplyRouteCollection>()
                    .ReadWriteShared<SupplyRouteRenderCollection>()
                    .ReadWriteShared<SupplyRoutePools>());
        }

        static private unsafe void UpdateRouteRenderers(float dt) {
            Find.State(out SupplyRouteCollection routes, out SupplyRouteRenderCollection renders, out SupplyRoutePools pools);
            Find.State(out SupplyRouteDrawingState draw);
            Find.GlobalAsset(out SupplyRouteConfig config);

            int drawingRoute = draw.RouteIndex;
            bool drawingRoutePreviewUpdated = draw.PreviewDirty;

            foreach(var routeIndex in routes.UpdatedRouteMask) {
                if (routeIndex == drawingRoute && drawingRoutePreviewUpdated) {
                    continue;
                }

                UpdateRouteLine(routeIndex, default, routes, renders, pools, config);
            }

            if (drawingRoutePreviewUpdated && drawingRoute >= 0) {
                UpdateRouteLine(drawingRoute, draw.HoverAction, routes, renders, pools, config);
            }
        }

        static private unsafe void UpdateRouteLine(int routeIndex, SupplyRouteDrawAction drawAction, SupplyRouteCollection routes, SupplyRouteRenderCollection renders, SupplyRoutePools pools, SupplyRouteConfig config) {
            ref SupplyRouteRenderer line = ref renders.Routes[routeIndex];
            if (line == null) {
                line = pools.LinePool.Alloc();
                Find.State(out SupplyShipIndex ships);
                Color lineColor = ships.ShipAssets[routeIndex].LineColor;
                line.StaticLine.startColor = line.StaticLine.endColor
                    = line.ReturnLine.startColor = line.ReturnLine.endColor = lineColor;

                SupplyRouteLineConfig.Apply(line.StaticLine, config.SolidLine);
                SupplyRouteLineConfig.Apply(line.ReturnLine, config.ReturnLine);
            }

            SupplyRouteData routeData;
            switch (drawAction) {
                case SupplyRouteDrawAction.RemoveLastNode:
                case SupplyRouteDrawAction.RemoveSegment:
                    routeData = routes.TempRouteBuffer;
                    break;

                default:
                    routeData = routes.Routes[routeIndex];
                    break;
            }

            bool autoConnected = (routeData.Flags & SupplyRouteFlags.AutoConnectEnd) != 0;
            bool isClosed = autoConnected || (routeData.NodeCount > 1 && routeData.Nodes[0] == routeData.Nodes[routeData.NodeCount - 1]);
            int segmentCount = autoConnected ? routeData.NodeCount : routeData.NodeCount - 1;
            bool overlappingSegments = isClosed && segmentCount == 2;

            Vector2 overlapOffset = default;

            if (routeData.NodeCount > 1) {
                Vector3* positions = stackalloc Vector3[routeData.NodeCount + 1];
                int positionCount = routeData.NodeCount;
                for (int i = 0; i < routeData.NodeCount; i++) {
                    positions[i] = (Vector2) routeData.Nodes[i].transform.localPosition;
                }

                if (overlappingSegments) {
                    Vector2 ba = (positions[1] - positions[0]).normalized * 0.2f;
                    overlapOffset = new Vector2(ba.y, -ba.x);

                    if (!autoConnected) {
                        positionCount++;
                        for(int i = positionCount; i > 1; i--) {
                            positions[i] = positions[i - 1] - (Vector3) overlapOffset;
                        }
                    }

                    positions[0] += (Vector3) overlapOffset;
                    positions[1] += (Vector3) overlapOffset;
                }

                line.StaticLine.positionCount = positionCount;
                line.StaticLine.SetPositions(Unsafe.NativeArray(positions, positionCount));
                line.StaticLine.enabled = true;

            } else {
                line.StaticLine.enabled = false;
            }

            if ((routeData.Flags & SupplyRouteFlags.AutoConnectEnd) != 0) {
                Vector2 a = (Vector2) routeData.Nodes[routeData.NodeCount - 1].transform.localPosition;
                Vector2 b = (Vector2) routeData.Nodes[0].transform.localPosition;

                line.ReturnLine.positionCount = 2;
                line.ReturnLine.SetPosition(0, a - overlapOffset);
                line.ReturnLine.SetPosition(1, b - overlapOffset);
                line.ReturnLine.enabled = true;
            } else {
                line.ReturnLine.enabled = false;
            }
        }

        static private void UpdateFragmentPreviews(float dt) {
            Find.State(out SupplyRouteCollection routes, out SupplyRouteRenderCollection renders, out SupplyRouteDrawingState draw, out SupplyRoutePools pools);
            Find.GlobalAsset(out SupplyRouteConfig config);
            Find.State(out SupplyChainMap map);

            if (routes.AreFragmentsDirty) {
                if (renders.TempFragmentDisable != null) {
                    renders.TempFragmentDisable.StaticLine.enabled = true;
                    renders.TempFragmentDisable = null;
                }

                while(renders.Fragments.TryPopFront(out SupplyRouteFragmentRenderer fragment)) {
                    Pool.TryFree(fragment);
                }

                foreach(var fragmentData in routes.Fragments) {
                    SupplyRouteFragmentRenderer fragment = pools.FragmentPool.Alloc();
                    SupplyRouteUtility.UpdateFragmentRendererPoints(fragment, map, fragmentData);
                    renders.Fragments.PushBack(fragment);
                }

                if (routes.TempRouteFragmentConsume >= 0) {
                    renders.TempFragmentDisable = renders.Fragments[routes.TempRouteFragmentConsume];
                    renders.TempFragmentDisable.StaticLine.enabled = false;
                }
            }

            if (draw.PreviewDirty) {
                if (renders.TempFragmentCreate) {
                    if (routes.TempRouteFragmentCreate.NodeCount == 0) {
                        Pool.TryFree(renders.TempFragmentCreate);
                        renders.TempFragmentCreate = null;
                    } else if (routes.TempRouteFragmentCreate.Key != renders.TempFragmentCreate.Key) {
                        SupplyRouteUtility.UpdateFragmentRendererPoints(renders.TempFragmentCreate, map, routes.TempRouteFragmentCreate);
                    }
                } else if (routes.TempRouteFragmentCreate.Key != 0) {
                    renders.TempFragmentCreate = pools.FragmentPool.Alloc();
                    SupplyRouteUtility.UpdateFragmentRendererPoints(renders.TempFragmentCreate, map, routes.TempRouteFragmentCreate);
                }

                SupplyRouteFragmentRenderer fragmentToHide = routes.TempRouteFragmentConsume >= 0 ? renders.Fragments[routes.TempRouteFragmentConsume] : null;
                if (renders.TempFragmentDisable != fragmentToHide) {
                    if (renders.TempFragmentDisable != null) {
                        renders.TempFragmentDisable.StaticLine.enabled = true;
                    }
                    renders.TempFragmentDisable = fragmentToHide;
                    if (renders.TempFragmentDisable != null) {
                        renders.TempFragmentDisable.StaticLine.enabled = false;
                    }
                }
            }
        }
    }
}
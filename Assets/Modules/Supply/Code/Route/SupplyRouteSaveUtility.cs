using BeauUtil;
using BeauUtil.Debugger;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    /// <summary>
    /// Moves drawn routes between SupplyRouteCollection and the SupplyMinigameState mirror that
    /// ImportState/ExportState persist.
    /// Capture runs on minigame exit, when the collection is still live. Apply runs from
    /// SupplyLoader once the map is built - it cannot run at import time, because id resolution,
    /// path evaluation and the ship list all need nodes that SupplyLoader has not activated yet.
    /// </summary>
    public static class SupplyRouteSaveUtility {
        // Scratch for resolving one route's nodes before committing it. Sized to the route cap.
        private static readonly SupplyRouteNode[] s_NodeScratch = new SupplyRouteNode[SupplyRouteData.MaxNodes];

        // Snapshots the finalized routes into the mirror. A route still open for drawing is
        // discarded rather than saved half-drawn, and a route with fewer than 2 nodes is nothing.
        public static unsafe void Capture(SupplyRouteCollection routes, SupplyRouteDrawingState draw, SupplyShipIndex ships, SupplyMinigameState supplyState) {
            supplyState.SavedRouteCount = Math.Min(ships.ShipCount, SupplyRouteData.MaxShips);

            for (int routeIdx = 0; routeIdx < SupplyRouteData.MaxShips; routeIdx++) {
                SupplyRouteSaveData routeSave = default;

                SupplyRouteData routeData = routes.Routes[routeIdx];
                bool captureable = routeIdx < ships.ShipCount
                    && routeIdx != draw.RouteIndex
                    && routeData.NodeCount >= 2;

                if (captureable) {
                    routeSave.NodeCount = (byte) routeData.NodeCount;
                    routeSave.Flags = routeData.Flags;
                    for (int nodeIdx = 0; nodeIdx < routeData.NodeCount; nodeIdx++) {
                        routeSave.NodeIds[nodeIdx] = routeData.Nodes[nodeIdx].Id.HashValue;
                    }
                }

                supplyState.SavedRoutes[routeIdx] = routeSave;
            }
        }

        // Rebuilds the live routes from the mirror and re-derives their stats. Route lines and ship
        // list readouts are left to the systems that already watch UpdatedRouteMask.
        public static unsafe void Apply(SupplyMinigameState supplyState, SupplyRouteCollection routes, SupplyShipIndex ships, SupplyChainMap map, ShipListPanel shipList, ShoppingListState shoppingList, SupplyProgressMeterState meter) {
            // TryEvaluatePath linecasts against the hazard colliders SupplyLoader just activated and
            // repositioned, and the project runs with Physics2D.autoSyncTransforms off, so those
            // colliders are still at their old transforms until this sync.
            Physics2D.SyncTransforms();

            int homeIndex = map.Home ? map.Home.Index : -1;
            int routeCount = Math.Min(supplyState.SavedRouteCount, ships.ShipCount);

            // Nodes already taken by an applied route, so a corrupt save can't produce the
            // overlapping routes the drawing rules forbid. Home is the shared start/end of every
            // route and is excluded.
            BitSet32 claimedMask = default;
            bool appliedAny = false;

            for (int routeIdx = 0; routeIdx < routeCount; routeIdx++) {
                SupplyRouteSaveData routeSave = supplyState.SavedRoutes[routeIdx];
                if (routeSave.NodeCount < 2) {
                    continue;
                }

                if (!TryResolveNodes(routeSave, map, claimedMask, homeIndex)) {
                    Log.Warn("[SupplyRouteSaveUtility] Dropping saved route {0} - nodes did not resolve against the current map", routeIdx);
                    // TODO: how do we want to handle this?
                    continue;
                }

                ref SupplyRouteData routeData = ref routes.Routes[routeIdx];
                routeData.Clear();
                routeData.NodeCount = routeSave.NodeCount;
                routeData.Flags = routeSave.Flags;
                for (int nodeIdx = 0; nodeIdx < routeSave.NodeCount; nodeIdx++) {
                    SupplyRouteNode node = s_NodeScratch[nodeIdx];
                    routeData.Nodes[nodeIdx] = node;
                    routeData.NodeMask.Set(node.Index);
                    if (node.Index != homeIndex) {
                        claimedMask.Set(node.Index);
                    }
                }

                SupplyRouteUtility.TryEvaluatePath(routeData, ships.ShipStats[routeIdx], routeIdx, out routes.RouteStats[routeIdx]);

                // UpdatedRouteMask is wiped every PreUpdate, and this runs during preload - the
                // pending mask survives to the next Supply frame, where the render and ship list
                // systems pick the route up the same way they do for a freshly drawn one.
                routes.PendingRouteRefreshMask.Set(routeIdx);

                // PopulateShipList hides every row's stats layer; nothing else turns it back on
                // outside of a route being drawn and closed.
                if (routes.RouteStats[routeIdx].Time > 0) {
                    ShipListRow row = shipList.Rows[routeIdx];
                    SupplyChainUtility.SetShipRowStatsActive(row, true);
                    SupplyChainUtility.SyncShipRowPositions(row);
                }

                appliedAny = true;
            }

            if (appliedAny) {
                SupplyChainUtility.ReflowShipList(shipList, true);
                shoppingList.Dirty = true;
                meter.NeedsRefresh = true;
            }
        }

        // Resolves a saved route's node ids into s_NodeScratch. Fails the whole route if any node is
        // missing from the map, inactive for this chapter, or already used by another route.
        private static unsafe bool TryResolveNodes(SupplyRouteSaveData routeSave, SupplyChainMap map, BitSet32 claimedMask, int homeIndex) {
            for (int nodeIdx = 0; nodeIdx < routeSave.NodeCount; nodeIdx++) {
                StringHash32 nodeId = new StringHash32(routeSave.NodeIds[nodeIdx]);
                if (!SupplyRouteUtility.TryGetNodeForId(map, nodeId, out SupplyRouteNode node)) {
                    return false;
                }

                if (!node.gameObject.activeSelf) {
                    return false;
                }

                if (node.Index != homeIndex && claimedMask.IsSet(node.Index)) {
                    return false;
                }

                s_NodeScratch[nodeIdx] = node;
            }

            return true;
        }
    }
}

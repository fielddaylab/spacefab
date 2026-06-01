using BeauUtil;
using FieldDay;
using Leaf.Runtime;

namespace SpaceFab.Supply {
    /// <summary>
    /// Leaf-callable queries and commands specific to the Supply Chain minigame.
    /// </summary>
    public static class SupplyScripting {
        // Whether the given material is currently being collected by any finalized route. Scans the
        // per-ship route stats' MaterialHashes (the same source ShoppingListLoadUtility reads to mark
        // shopping-list rows collected). Returns false when no route collection is present.
        [LeafMember("IsMaterialCollected")]
        public static unsafe bool Leaf_IsMaterialCollected(StringHash32 materialId) {
            if (materialId.IsEmpty) { return false; }
            if (!Game.SharedState.Has<SupplyRouteCollection>()) { return false; }

            SupplyRouteCollection routes = Find.State<SupplyRouteCollection>();
            if (routes.RouteStats == null) { return false; }

            uint wanted = materialId.HashValue;
            for (int shipIdx = 0; shipIdx < routes.RouteStats.Length; shipIdx++) {
                SupplyRouteStats stats = routes.RouteStats[shipIdx];
                for (int matIdx = 0; matIdx < SupplyRouteData.MaxCapacity; matIdx++) {
                    if (stats.MaterialHashes[matIdx] == wanted) { return true; }
                }
            }
            return false;
        }
    }
}

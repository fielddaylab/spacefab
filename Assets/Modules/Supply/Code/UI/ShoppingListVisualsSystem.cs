using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Supply
{
    /// <summary>
    /// Rebuilds the shopping list's rows when its state is dirty: regenerates
    /// one row per contract requirement and fills each slot with a gathered
    /// material that satisfies it. Leaves the Dirty flag for
    /// ShoppingListRefreshSystem to clear in LateUpdate.
    /// </summary>
    public class ShoppingListVisualsSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadWriteShared<ShoppingListState>()
                    .ReadWriteShared<ShoppingListLayoutState>()
                    .ReadShared<SupplyRouteCollection>()
                    .ReadShared<PlayerProgressState>()
            );
        }


        static private void ProcessWork(float deltaTime)
        {
            Find.State(
                out ShoppingListState shoppingState,
                out ShoppingListLayoutState layoutState,
                out SupplyRouteCollection routes,
                out PlayerProgressState progressState
                );

            if (!shoppingState.Dirty) { return; }

            // ShoppingListLoadUtility scans the finalized routes' gathered
            // materials and, for each contract requirement, fills the slot via
            // ContractProgressUtility.FindFulfillingMaterials — player-confirmed
            // knowledge, so only materials the player has researched enough to
            // satisfy the check count.
            ShoppingListLoadUtility.Rebuild(layoutState, routes, progressState);
        }
    }
}

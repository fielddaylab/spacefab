using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Supply
{
    /// <summary>
    /// Clears the shopping list's one-frame Dirty flag after
    /// ShoppingListVisualsSystem has consumed it earlier in the frame.
    /// </summary>
    public class ShoppingListRefreshSystem : SystemComponent
    {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs)
        {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 100, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadWriteShared<ShoppingListState>()
            );
        }

        // Clears the Dirty flag once the rebuild has run for this frame.
        static private void ProcessWork(float deltaTime)
        {
            ShoppingListState shoppingState = Find.State<ShoppingListState>();
            shoppingState.Dirty = false;
        }
    }
}

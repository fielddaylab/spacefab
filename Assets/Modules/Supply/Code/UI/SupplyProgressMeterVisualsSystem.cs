using FieldDay;
using FieldDay.Systems;

namespace SpaceFab.Supply {
    /// <summary>
    /// Refreshes the Supply mini progress meter. Polls the (pending-aware) route aggregate
    /// each tick and rebuilds the panel only when it changes or a refresh was forced — this
    /// covers load and every route create / modify / remove without a dedicated route event.
    /// Expand/collapse is driven by the toggle button, not this system.
    /// </summary>
    public class SupplyProgressMeterVisualsSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.Update, 0, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadWriteShared<SupplyProgressMeterState>()
                    .ReadWriteShared<SupplyProgressMeterLayoutState>()
                    .ReadShared<SupplyRouteCollection>()
                    .ReadShared<SupplyRouteDrawingState>()
                    .ReadShared<PlayerProgressState>()
            );
        }

        static private void ProcessWork(float deltaTime) {
            Find.State(
                out SupplyProgressMeterState meterState,
                out SupplyProgressMeterLayoutState layoutState,
                out SupplyRouteCollection routes,
                out SupplyRouteDrawingState drawing
                );
            ContractState contract = Find.State<ContractState>();
            SupplyShipIndex ships = Find.State<SupplyShipIndex>();

            // Diff the aggregate against the last-applied values; skip the rebuild if unchanged.
            SupplyProgressMeterUtility.ComputeAggregate(routes, drawing, out int risk, out int cost, out int time, out int activeMask);
            bool changed = meterState.NeedsRefresh
                || risk != meterState.LastRisk
                || cost != meterState.LastCost
                || time != meterState.LastTime
                || activeMask != meterState.LastActiveMask;
            if (!changed) {
                return;
            }

            SupplyProgressMeterUtility.Refresh(layoutState, routes, drawing, ships, contract);

            meterState.LastRisk = risk;
            meterState.LastCost = cost;
            meterState.LastTime = time;
            meterState.LastActiveMask = activeMask;
            meterState.NeedsRefresh = false;
        }
    }
}

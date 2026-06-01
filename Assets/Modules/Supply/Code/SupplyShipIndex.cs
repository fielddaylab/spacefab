using FieldDay.SharedState;
using System;

namespace SpaceFab.Supply {
    public sealed class SupplyShipIndex : SharedStateComponent {
        [NonSerialized] public int ShipCount = 0;
        [NonSerialized] public SupplyShipStats[] ShipStats = new SupplyShipStats[SupplyRouteData.MaxShips];
        [NonSerialized] public SupplyShipAsset[] ShipAssets = new SupplyShipAsset[SupplyRouteData.MaxShips];
    }
}
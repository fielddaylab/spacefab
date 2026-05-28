using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteCollection : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public SupplyRouteData[] Routes;
        [NonSerialized] public SupplyRouteStats[] RouteStats;

        [NonSerialized] public RingBuffer<SupplyRouteFragmentData> Fragments;
        [NonSerialized] public BitSet32 FragmentNodesMask;

        [NonSerialized] public SupplyRouteData TempRouteBuffer;
        [NonSerialized] public SupplyRouteStats TempRouteStats;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Fragments = new RingBuffer<SupplyRouteFragmentData>(SupplyRouteData.MaxNodes - 1, RingBufferMode.Expand);
            Routes = new SupplyRouteData[SupplyRouteData.MaxShips];
            RouteStats = new SupplyRouteStats[SupplyRouteData.MaxShips];

            for(int i = 0; i < SupplyRouteData.MaxShips; i++) {
                Routes[i].Create();
            }

            TempRouteBuffer.Create();
        }
    }

    static public partial class SupplyRouteUtility {
        
    }
}
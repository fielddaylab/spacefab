using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteCollection : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public SupplyRouteData[] Routes;
        [NonSerialized] public RingBuffer<SupplyRouteFragmentData> Fragments;
        [NonSerialized] public BitSet32 FragmentNodesMask;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Fragments = new RingBuffer<SupplyRouteFragmentData>(SupplyRouteData.MaxNodes - 1, RingBufferMode.Expand);
            Routes = new SupplyRouteData[SupplyRouteData.MaxShips];

            for(int i = 0; i < SupplyRouteData.MaxShips; i++) {
                Routes[i].Create();
            }
        }
    }
}
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteRenderCollection : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public SupplyRouteRenderer[] Routes;
        [NonSerialized] public RingBuffer<SupplyRouteFragmentRenderer> Fragments;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Fragments = new RingBuffer<SupplyRouteFragmentRenderer>(SupplyRouteData.MaxNodes - 1);
            Routes = new SupplyRouteRenderer[SupplyRouteData.MaxShips];
        }
    }
}
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteDrawingState : SharedStateComponent, IRegistrationCallbacks {
        public LineRenderer CursorLine;
        public LineRenderer PreviewDeleteLine;

        [NonSerialized] public SupplyRouteDrawPhase Phase;
        [NonSerialized] public int RouteIndex = -1;

        [NonSerialized] public SupplyRouteData PreviewRouteData;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            PreviewRouteData.Create();
        }
    }

    public enum SupplyRouteDrawPhase {
        Unselected,
        Drawing,
        Previewing
    }
}
using BeauPools;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Components;
using System;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteFragmentRenderer : BatchedComponent, IPoolAllocHandler {
        public LineRenderer StaticLine;

        [NonSerialized] public ushort Key;
        [NonSerialized] public AnimHandle FlashAnim;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            Game.Animation.CancelAnimation(ref FlashAnim);
        }
    }
}
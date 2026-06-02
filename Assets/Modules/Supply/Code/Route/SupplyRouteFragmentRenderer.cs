using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
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

    static public partial class SupplyRouteUtility {
        static public unsafe void UpdateFragmentRendererPoints(SupplyRouteFragmentRenderer renderer, SupplyChainMap map, in SupplyRouteFragmentData fragmentData) {
            int pointCount = fragmentData.NodeCount;
            Assert.True(pointCount >= 2);
            Vector3* points = stackalloc Vector3[pointCount];
            for(int i = 0; i < pointCount; i++) {
                int nodeIndex = fragmentData.Nodes[i];
                points[i] = map.Nodes[nodeIndex].Position;
            }
            renderer.StaticLine.SetPositions(Unsafe.NativeArray(points, pointCount));
            renderer.Key = fragmentData.Key;
        }
    }
}
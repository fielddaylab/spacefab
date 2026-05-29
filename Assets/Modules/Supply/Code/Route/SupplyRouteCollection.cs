using BeauUtil;
using BeauUtil.Debugger;
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
        [NonSerialized] public int TempRouteFragmentConsume = -1;
        [NonSerialized] public SupplyRouteFragmentData TempRouteFragmentCreate;

        [NonSerialized] public BitSet32 UpdatedRouteMask;
        [NonSerialized] public bool AreFragmentsDirty;

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

    public enum FragmentFindResult {
        None,
        First,
        Last,
        Middle
    }

    static public partial class SupplyRouteUtility {
        static public unsafe FragmentFindResult TryFindFragment(SupplyRouteNode node, out int fragmentIndex) {
            Find.State(out SupplyRouteCollection routes);

            int nodeIndex = node.Index;
            if (!routes.FragmentNodesMask.IsSet(nodeIndex)) {
                fragmentIndex = -1;
                return FragmentFindResult.None;
            }

            for(int fragIdx = 0; fragIdx < routes.Fragments.Count; fragIdx++) {
                SupplyRouteFragmentData fragmentData = routes.Fragments[fragIdx];
                if (fragmentData.Nodes[0] == nodeIndex) {
                    fragmentIndex = fragIdx;
                    return FragmentFindResult.First;
                }
                if (fragmentData.Nodes[fragmentData.NodeCount - 1] == nodeIndex) {
                    fragmentIndex = fragIdx;
                    return FragmentFindResult.Last;
                }

                for (int fragNodeIdx = 1; fragNodeIdx < fragmentData.NodeCount - 1; fragNodeIdx++) {
                    if (fragmentData.Nodes[fragNodeIdx] == nodeIndex) {
                        fragmentIndex = fragIdx;
                        return FragmentFindResult.Middle;
                    }
                }
            }

            Assert.Fail("Fragment node mask indicated that node was present, but not found in fragments");
            fragmentIndex = -1;
            return FragmentFindResult.None;
        }
        
        static public unsafe void AddFragment(SupplyRouteFragmentData fragmentData) {
            Find.State(out SupplyRouteCollection routes);

            Assert.True(fragmentData.NodeCount > 1, "Fragment is not valid");

            BitSet32 fragmentNodeMask = routes.FragmentNodesMask;

            for(int i = 0; i < fragmentData.NodeCount; i++) {
                int nodeIndex = fragmentData.Nodes[i];
                Assert.False(fragmentNodeMask.IsSet(nodeIndex), "Overlapping fragments! How?");
                fragmentNodeMask.Set(nodeIndex);
            }

            routes.Fragments.PushBack(fragmentData);
            routes.FragmentNodesMask = fragmentNodeMask;
            routes.AreFragmentsDirty = true;
        }

        static public unsafe void RemoveFragmentAtIndex(int fragmentIndex) {
            Find.State(out SupplyRouteCollection routes);

            SupplyRouteFragmentData fragmentData = routes.Fragments[fragmentIndex];
            routes.Fragments.FastRemoveAt(fragmentIndex);

            BitSet32 fragmentNodeMask = routes.FragmentNodesMask;

            for (int i = 0; i < fragmentData.NodeCount; i++) {
                int nodeIndex = fragmentData.Nodes[i];
                Assert.True(fragmentNodeMask.IsSet(nodeIndex), "Fragment was already removed?");
                fragmentNodeMask.Unset(nodeIndex);
            }

            routes.FragmentNodesMask = fragmentNodeMask;
            routes.AreFragmentsDirty = true;
        }

        static public void ClearFragments() {
            Find.State(out SupplyRouteCollection routes);

            routes.Fragments.Clear();
            routes.FragmentNodesMask = default;
            routes.AreFragmentsDirty = true;
        }

        static public bool IsNodeInOtherRoutes(SupplyRouteNode node, int excludeRouteIndex, out int overlapRouteIndex) {
            Find.State(out SupplyRouteCollection routes, out SupplyShipIndex ships);

            int nodeIndex = node.Index;

            for(int i = 0; i < ships.ShipCount; i++) {
                if (i == excludeRouteIndex) {
                    continue;
                }

                if (routes.Routes[i].NodeMask.IsSet(nodeIndex)) {
                    overlapRouteIndex = i;
                    return true;
                }
            }

            overlapRouteIndex = -1;
            return false;
        }
    }
}
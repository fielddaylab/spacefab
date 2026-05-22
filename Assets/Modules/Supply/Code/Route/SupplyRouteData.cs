using BeauUtil;
using System;

namespace SpaceFab.Supply {
    public struct SupplyRouteData {
        public const int MaxNodes = 16;
        public const int MaxHazards = 16;
        public const int MaxShips = 5;
        public const int MaxCapacity = 3;

        public int NodeCount;
        public SupplyRouteNode[] Nodes;
        public BitSet32 NodeMask;

        // TODO: Implement hazards
        
        public void Create() {
            Nodes = new SupplyRouteNode[MaxNodes];
        }
        
    }

    public unsafe struct SupplyRouteFragmentData {
        public byte NodeCount;
        public fixed byte Nodes[SupplyRouteData.MaxNodes - 1];

        public ushort Key {
            get {
                if (NodeCount < 2) {
                    return 0;
                }

                return (ushort) (Nodes[0] << 8 | Nodes[NodeCount - 1]);
            }
        }
    }

    public unsafe struct SupplyRouteStats {
        public SupplyRouteFlags Flags;
        public byte Time;
        public byte Cost;
        public byte Risk;
        public fixed uint MaterialHashes[SupplyRouteData.MaxCapacity + 1];
    }

    [Flags]
    public enum SupplyRouteFlags : byte {
        Success = 0,

        TooManyResources = 0x01,
        NodeInAnotherPath = 0x02,
        NodeInInvalidSegment = 0x04,
        NodeInEarlierSegment = 0x08,
        PathTravelsThroughAnotherNode = 0x10
    }
}
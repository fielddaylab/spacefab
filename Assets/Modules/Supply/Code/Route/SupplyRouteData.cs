using BeauUtil;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteData {
        public const int MaxNodes = 16;
        public const int MaxHazards = 16;

        public int NodeCount;
        public SupplyRouteNode[] Nodes = new SupplyRouteNode[MaxNodes];
        public BitSet32 NodeMask;
        
        // TODO: Implement hazards
    }

    public struct SupplyRouteFragmentData {
        public byte NodeCount;
        public unsafe fixed byte Nodes[SupplyRouteData.MaxNodes];
        public BitSet32 NodeMask;
    }
}
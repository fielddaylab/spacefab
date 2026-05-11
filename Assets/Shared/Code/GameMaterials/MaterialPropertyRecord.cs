using System;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Per-material confirmed-property bitmasks.
    /// StaticMask: bit i = StaticProperty i confirmed (10 of 16 bits used).
    /// DynamicMask0/1: bit i = the corresponding DynamicProperty confirmed against
    /// the material at MaterialOrderAsset index i.
    /// </summary>
    [Serializable]
    public struct MaterialPropertyRecord
    {
        public ushort StaticMask;
        public ushort DynamicMask_PDopant;
        public ushort DynamicMaskNDopant;
    }
}

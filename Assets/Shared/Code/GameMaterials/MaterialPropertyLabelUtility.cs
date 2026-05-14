namespace SpaceFab.Materials
{
    /// <summary>
    /// Partitioning classifier over MaterialPropertyLabel. Tells callers which
    /// labels round-trip to PlayerProgressState and how their bits are laid out
    /// in MaterialPropertyRecord. Replaces the StaticProperty / DynamicProperty
    /// enums: the static/dynamic distinction lives here as a partition over the
    /// unified label enum, not as a parallel vocabulary.
    ///
    /// GetStaticBitIndex values are wire-format bit positions in
    /// MaterialPropertyRecord.StaticMask. They MUST remain stable - changing one
    /// invalidates every saved game. Reserve new indices by appending to the
    /// switch, never by reordering.
    /// </summary>
    public static class MaterialPropertyLabelUtility
    {
        /// <summary>
        /// True for labels that represent a confirmable Property (round-trips to
        /// PlayerProgressState). False for observation-only labels (evidence
        /// collected in chambers; never persisted).
        /// </summary>
        public static bool IsPersistent(MaterialPropertyLabel label)
        {
            switch (label)
            {
                case MaterialPropertyLabel.ConductorNaive:
                case MaterialPropertyLabel.InsulatorNaive:
                case MaterialPropertyLabel.Insulator:
                case MaterialPropertyLabel.Conductor:
                case MaterialPropertyLabel.Semiconductor:
                case MaterialPropertyLabel.HiTempConductor:
                case MaterialPropertyLabel.HiTempSemiConductor:
                case MaterialPropertyLabel.LightEmittingSemiconductor:
                case MaterialPropertyLabel.HighVoltageSemiconductor:
                case MaterialPropertyLabel.HighMobilitySemiconductor:
                case MaterialPropertyLabel.PDopantFor:
                case MaterialPropertyLabel.NDopantFor:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True for labels that are parameterized by a context material (e.g.
        /// PDopantFor X). Dynamic labels store their bit in
        /// MaterialPropertyRecord.DynamicMask_PDopant or DynamicMaskNDopant,
        /// indexed by MaterialOrderAsset position.
        /// </summary>
        public static bool IsDynamic(MaterialPropertyLabel label)
        {
            switch (label)
            {
                case MaterialPropertyLabel.PDopantFor:
                case MaterialPropertyLabel.NDopantFor:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Bit position in MaterialPropertyRecord.StaticMask for a static
        /// persistent label. Returns -1 for non-static-persistent labels
        /// (observations or dynamic labels). Bit positions are wire-format and
        /// must remain stable across versions.
        /// </summary>
        public static int GetStaticBitIndex(MaterialPropertyLabel label)
        {
            switch (label)
            {
                case MaterialPropertyLabel.ConductorNaive: return 0;
                case MaterialPropertyLabel.InsulatorNaive: return 1;
                case MaterialPropertyLabel.Insulator: return 2;
                case MaterialPropertyLabel.Conductor: return 3;
                case MaterialPropertyLabel.Semiconductor: return 4;
                case MaterialPropertyLabel.HiTempConductor: return 5;
                case MaterialPropertyLabel.HiTempSemiConductor: return 6;
                case MaterialPropertyLabel.LightEmittingSemiconductor: return 7;
                case MaterialPropertyLabel.HighVoltageSemiconductor: return 8;
                case MaterialPropertyLabel.HighMobilitySemiconductor: return 9;
                default: return -1;
            }
        }
    }
}

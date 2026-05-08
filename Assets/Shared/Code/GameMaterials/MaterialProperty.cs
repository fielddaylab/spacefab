namespace SpaceFab.Materials
{
    /// <summary>
    /// Global static property types confirmable for any material.
    /// Enum value = bit position in MaterialPropertyRecord.StaticMask.
    /// </summary>
    public enum StaticProperty : byte
    {
        Insulator_Naive,
        Conductor_Naive,
        Insulator,
        Conductor,
        Semiconductor,
        HiTempConductor,
        HiTempSemiconductor,
        LightEmittingSemiconductor,
        HiVoltageSemiconductor,
        HiMobilitySemiconductor,
    }

    /// <summary>
    /// Global dynamic property types parameterized by another material.
    /// Enum value selects which DynamicMask field on MaterialPropertyRecord stores it.
    /// </summary>
    public enum DynamicProperty : byte
    {
        PDopantForX,
        NDopantForX,
    }
}

using BeauUtil;
using FieldDay.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Materials
{
    public enum MaterialPropertyLabel : uint
    {
        Conductive,
        NonConductive,
        HeatActivated,
        HeatDeactivated,
        HeatUnaffected,
        HeatVulnerable,
        HeatResistant,
        AtomicRadiusLessThan,
        AtomicRadiusGreaterThan,
        LightEmitting,
        HighMobility,
        VoltageResistant,

        ConductorNaive,
        InsulatorNaive,
        Insulator,
        Conductor,
        Semiconductor,
        HiTempConductor,
        HiTempSemiConductor,
        PDopantFor,
        NDopantFor,
        LightEmittingSemiconductor,
        HighVoltageSemiconductor,
        HighMobilitySemiconductor,
    }

    public enum ChamberType
    { 
        Electrical,
        Thermal,
        Dopant,
        Special,
        ConfirmedProperty,
    }

    [CreateAssetMenu(menuName = "SpaceFab/Material Property")]
    public class MaterialPropertyCheck : ScriptableObject
    {
        public MaterialPropertyLabel Label;
        public MaterialObservationCheck[] RequiredObservations;
        [AssetName(typeof(MaterialAsset))] public StringHash32 InComparisonTo;
    }

    /// <summary>
    /// Lighter footprint than MaterialPropertyCheck
    /// </summary>
    public class MaterialProperty
    {
        public MaterialPropertyLabel Label;
        public MaterialObservation[] RequiredObservations;
        [AssetName(typeof(MaterialAsset))] public StringHash32 InComparisonTo;

        public MaterialProperty(MaterialPropertyLabel label, MaterialObservation[] requiredObservations)
        {
            Label = label;
            RequiredObservations = requiredObservations;
            InComparisonTo = StringHash32.Null;
        }

        public MaterialProperty(MaterialPropertyLabel label, MaterialObservation[] requiredObservations, StringHash32 inComparisonTo)
        {
            Label = label;
            RequiredObservations = requiredObservations;
            InComparisonTo = inComparisonTo;
        }
    }
}
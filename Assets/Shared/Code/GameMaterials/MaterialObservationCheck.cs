using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Materials;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Materials
{
    [CreateAssetMenu(menuName = "SpaceFab/Material Observation")]
    public class MaterialObservationCheck : ScriptableObject
    {
        public MaterialPropertyLabel Label;
        public ChamberType ChamberType;
        [AssetName(typeof(MaterialAsset))] public StringHash32 InComparisonTo;
    }

    /// <summary>
    /// Lighter footprint than MaterialObservationCheck
    /// </summary>
    public class MaterialObservation
    {
        public MaterialPropertyLabel Label;
        public ChamberType ChamberType;
        [AssetName(typeof(MaterialAsset))] public StringHash32 InComparisonTo;

        public MaterialObservation(MaterialPropertyLabel label, ChamberType chamberType, StringHash32 inComparisonTo)
        {
            Label = label;
            ChamberType = chamberType;
            InComparisonTo = inComparisonTo;
        }
    }

    public static class MaterialUtility
    {
        public static bool Equals(MaterialObservationCheck checkAgainst, MaterialObservation toCheck)
        {
            return toCheck.Label == checkAgainst.Label && toCheck.InComparisonTo.Equals(checkAgainst.InComparisonTo);
        }

        public static bool Equals(MaterialPropertyCheck checkAgainst, MaterialProperty toCheck)
        {
            return toCheck.Label == checkAgainst.Label && toCheck.InComparisonTo.Equals(checkAgainst.InComparisonTo);
        }
    }
}

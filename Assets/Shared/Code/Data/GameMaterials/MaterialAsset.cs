using FieldDay.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.GameMaterials
{
    [Serializable]
    public struct AtomicRadius
    {
        public int PM;
        public int Calculated;
    }

    public enum MaterialType
    {
        Silicon,
        Boron,
        Phosphorus,
        Copper,
        SiliconCarbide,
        GalliumNitride,
        GalliumArsenide,
        Tungsten,
        Magnesium,
        Diamond
    }

    public enum MaterialStage
    {
        Default,
        Raw,
        Refined,
        FusedQuartz
    }

    [CreateAssetMenu(menuName = "SpaceFab/Game Material Asset")]
    public class MaterialAsset : NamedAsset
    {
        public string DisplayName;

        public MaterialStage Stage;
        public int[] ValenceElectronCounts;
        public AtomicRadius AtomicRadius;
    }
}

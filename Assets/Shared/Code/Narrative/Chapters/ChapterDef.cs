using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using Leaf;
using SpaceFab.Design;
using SpaceFab.Materials;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Chapter Asset")]
    public class ChapterDef : NamedAsset
    {
        [Header("Contracts")]
        [AssetName(typeof(ContractDef))] public StringHash32[] AvailableContracts;

        [Header("Materials")]
        [AssetName(typeof(MaterialAsset)), FormerlySerializedAs("m_availableMaterials")] public StringHash32[] AvailableMaterials;
        [AssetName(typeof(MaterialAsset)), FormerlySerializedAs("m_excludeFromResearch")] public StringHash32[] ExcludeFromResearch;

        [Header("Assets")]
        public LeafAsset Script;
    }
}
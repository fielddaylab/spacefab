using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using Leaf;
using SpaceFab.Design;
using SpaceFab.Materials;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Chapter Asset")]
    public class ChapterDef : NamedAsset
    {
        [Header("Contracts")]
        [AssetName(typeof(ContractDef))] public StringHash32[] AvailableContracts;
        [SerializeField, Range(0f, 1f)] public float[] GlitchChances;

        [Header("Materials")]
        [AssetName(typeof(MaterialAsset)), FormerlySerializedAs("m_availableMaterials")] public StringHash32[] AvailableMaterials;
        [AssetName(typeof(MaterialAsset)), FormerlySerializedAs("m_excludeFromResearch")] public StringHash32[] ExcludeFromResearch;

        [Header("Assets")]
        public LeafAsset Script;

        private void OnEnable()
        {
            foreach (StringHash32 contractId in AvailableContracts)
            {
                ContractDef contractDef = ContractUtility.GetDefinition(contractId);
                if (contractDef == null) continue;

                ContractAssetSet contractAssetSet = Find.NamedAsset<ContractAssetSet>(contractDef.AssetSet);
                if (contractAssetSet == null) continue;

                
                GlitchChances.Append(contractAssetSet.FabricationLevel.GlitchChance);
            }
        }
    }
}
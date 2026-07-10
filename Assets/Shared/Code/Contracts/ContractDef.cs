using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Design;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Contract Asset")]
    public class ContractDef : NamedAsset
    {
        [SerializeField] private MaterialPropertyCheck[] m_requiredMaterialProperties;
        // Subset of m_requiredMaterialProperties (same asset references) that the Supply Chain
        // shopping list should ignore. Research still uses the full required set; only Supply
        // subtracts these. Match is by asset reference.
        [SerializeField] private MaterialPropertyCheck[] m_omitFromSupplyRequirements;
        [SerializeField] private string m_title;
        [SerializeField] private string m_description;
        [SerializeField] private string m_client;
        [SerializeField] private int m_payout;
        [SerializeField] private int m_expectedDuration;
        [SerializeField] private int m_expectedProfit;

        [StreamedPackId] public StringHash32 StreamedPack;
        [AssetName(typeof(ContractAssetSet))] public StringHash32 AssetSet;

        public MaterialPropertyCheck[] RequiredMaterialProperties() { return m_requiredMaterialProperties; }
        public MaterialPropertyCheck[] OmitFromSupplyRequirements() { return m_omitFromSupplyRequirements; }
        // public StringHash32[] RequiredResearchMaterials() { return m_requiredResearchMaterials; }
        public string Title() { return m_title; }
        public string Description() { return m_description; }
        public string Client() { return m_client; }
        public int Payout() { return m_payout; }
        public int ExpectedDuration() { return m_expectedDuration; }
        public int ExpectedProfit() { return m_expectedProfit; }
    }
}
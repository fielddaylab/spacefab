using BeauUtil;
using FieldDay.Assets;
using SpaceFab.ChipDesign;
using SpaceFab.Materials;
using System;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Contract Asset")]
    public class ContractAsset : NamedAsset
    {
        [SerializeField] private int m_value;
        [SerializeField] private MaterialPropertyCheck[] m_requiredMaterialProperties;
        [AssetName(typeof(LevelData))][SerializeField] private StringHash32 m_designLevel;
        [SerializeField] private string m_title;
        [SerializeField] private string m_description;
        [SerializeField] private string m_client;
        [SerializeField] private int m_expectedDuration;
        [SerializeField] private int m_expectedProfit;

        public int Value() { return m_value; }
        public MaterialPropertyCheck[] RequiredMaterials() { return m_requiredMaterialProperties; }
        public StringHash32 DesignLevel() { return m_designLevel; }
        public string Title() { return m_title; }
        public string Description() { return m_description; }
        public string Client() { return m_client; }
        public int ExpectedDuration() { return m_expectedDuration; }
        public int ExpectedProfit() { return m_expectedProfit; }
    }
}
using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Design;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Chapter Asset")]
    public class ChapterDef : NamedAsset
    {
        // TODO: load contract assets on chapter start
        public AssetPack AvailableContracts; // Potential Contract assets (individually not necessarily loaded until selected)
        [AssetName(typeof(ContractsBundle))][SerializeField] public StringHash32 AvailableContractsBundleId;

        // public AssetPack ChapterAssets;  // All other chapter assets (always loaded for chapter)
        // [AssetName(typeof(ContractAsset))] public StringHash32[] Contracts;
    }
}
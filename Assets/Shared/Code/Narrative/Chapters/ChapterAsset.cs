using BeauUtil;
using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Chapter Asset")]
    public class ChapterAsset : NamedAsset
    {
        // TODO: load contract assets on chapter start
        public AssetPack ContractAssets;
        // [AssetName(typeof(ContractAsset))] public StringHash32[] Contracts;
    }
}
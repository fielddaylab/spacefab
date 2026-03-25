using FieldDay.Assets;
using SpaceFab.Design;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName ="SpaceFab/Overarching/Contract Asset Wrapper")]
    public class ContractAssetsWrapper : NamedAsset
    {
        public LevelData DesignLevelData;
    }
}

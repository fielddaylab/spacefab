using BeauUtil;
using FieldDay.Assets;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [Serializable]
    public struct ContractAssetsLookupEntry
    {
        [AssetName(typeof(ContractDef))] [SerializeField] public StringHash32 ContractId;
        public SceneReference Scene;
    }

    public class ContractAssetsLookup : SharedStateComponent
    {
        public ContractAssetsLookupEntry[] Entries;
    }
}

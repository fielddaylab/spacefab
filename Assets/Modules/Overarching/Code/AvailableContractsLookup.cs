using BeauUtil;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    [Serializable]
    public struct AvailableContractLookupEntry {
        public SceneReference Scene;
    }


    public class AvailableContractsLookup : SharedStateComponent
    {
        public AvailableContractLookupEntry[] Entries;
    }
}

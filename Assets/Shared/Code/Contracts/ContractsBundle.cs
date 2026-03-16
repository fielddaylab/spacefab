using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Contracts Bundle")]
    public class ContractsBundle : NamedAsset
    {
        public ContractDef[] AvailableContracts;
    }
}

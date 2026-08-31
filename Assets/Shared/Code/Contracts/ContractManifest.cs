using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    /// <summary>
    /// Defines the canonical ordering of all contracts for save serialization.
    /// The array index of each entry is its bit position in the completed-contracts bitmask.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Contracts/Contract Manifest")]
    public class ContractManifest : GlobalAsset {
        [AssetName(typeof(ContractDef))] public StringHash32[] Contracts;
    }

    static public partial class ContractUtility {
        static public int GetIndex(StringHash32 contractId) {
            Find.GlobalAsset(out ContractManifest manifest);
            for (int i = 0, len = manifest.Contracts.Length; i < len; i++) {
                if (manifest.Contracts[i] == contractId) {
                    return i;
                }
            }
            Assert.Fail("No contract with id '{0}'", contractId);
            return -1;
        }

        static public ContractDef GetDefinition(StringHash32 contractId) {
            return Find.NamedAsset<ContractDef>(contractId);
        }

        static public ContractDef GetDefinition(int contractIndex) {
            Find.GlobalAsset(out ContractManifest manifest);
            Assert.True(contractIndex >= 0 && contractIndex < manifest.Contracts.Length, "Contract index {0} out of range", contractIndex);
            return Find.NamedAsset<ContractDef>(manifest.Contracts[contractIndex]);
        }

        static public int ContractCount() {
            Find.GlobalAsset(out ContractManifest manifest);
            return manifest.Contracts.Length;
        }
    }
}
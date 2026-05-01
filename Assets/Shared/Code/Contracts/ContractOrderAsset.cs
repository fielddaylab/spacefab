using BeauUtil;
using FieldDay.Assets;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    /// <summary>
    /// Defines the canonical ordering of all contracts for save serialization.
    /// The array index of each entry is its bit position in the completed-contracts bitmask.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Contracts/Contract Order")]
    public class ContractOrderAsset : GlobalAsset
    {
        [AssetName(typeof(ContractDef))]
        [SerializeField] private StringHash32[] m_ContractIds;

        private Dictionary<StringHash32, int> m_IndexLookup;

        public override void Mount()
        {
            m_IndexLookup = new Dictionary<StringHash32, int>(m_ContractIds.Length);
            for (int i = 0; i < m_ContractIds.Length; i++)
                m_IndexLookup[m_ContractIds[i]] = i;
        }

        public override void Unmount() => m_IndexLookup = null;

        /// <summary>
        /// Returns the bit-mask index for the given contract id, or false if not found.
        /// </summary>
        public bool TryGetIndex(StringHash32 contractId, out int index)
            => m_IndexLookup.TryGetValue(contractId, out index);

        public StringHash32 GetId(int index) => m_ContractIds[index];
        public int Count => m_ContractIds.Length;
    }
}
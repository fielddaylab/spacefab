using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Materials;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    /// <summary>
    /// Defines the canonical ordering of all materials for save serialization.
    /// The array index of each entry is its bit position in the researched-materials bitmask.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Game Materials/Material Order")]
    public class MaterialOrderAsset : GlobalAsset
    {
        [AssetName(typeof(MaterialAsset))]
        [SerializeField] private StringHash32[] m_MaterialIds;

        private Dictionary<StringHash32, int> m_IndexLookup;

        public override void Mount()
        {
            m_IndexLookup = new Dictionary<StringHash32, int>(m_MaterialIds.Length);
            for (int i = 0; i < m_MaterialIds.Length; i++)
                m_IndexLookup[m_MaterialIds[i]] = i;
        }

        public override void Unmount() => m_IndexLookup = null;

        /// <summary>
        /// Returns the bitmask index for the given material id, or false if not found.
        /// </summary>
        public bool TryGetIndex(StringHash32 materialId, out int index)
            => m_IndexLookup.TryGetValue(materialId, out index);

        public StringHash32 GetId(int index) => m_MaterialIds[index];
        public int Count => m_MaterialIds.Length;
    }
}
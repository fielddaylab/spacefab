using FieldDay.Assets;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Global registry of MaterialPropertyDefinitions, keyed by
    /// MaterialPropertyLabel. There is exactly one definition per persistent
    /// property label. Observation labels have no entry here; they are
    /// leaves, not derived properties.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Game Materials/Property Definition Registry")]
    public class MaterialPropertyDefinitionAsset : GlobalAsset
    {
        [SerializeField] private MaterialPropertyDefinition[] m_Definitions;

        private Dictionary<MaterialPropertyLabel, MaterialPropertyDefinition> m_Lookup;

        public override void Mount()
        {
            m_Lookup = new Dictionary<MaterialPropertyLabel, MaterialPropertyDefinition>(m_Definitions.Length);
            for (int i = 0; i < m_Definitions.Length; i++)
            {
                MaterialPropertyDefinition def = m_Definitions[i];
                if (def == null) continue;
                m_Lookup[def.Label] = def;
            }
        }

        public override void Unmount() => m_Lookup = null;

        /// <summary>
        /// Returns the definition for the given persistent property label, or
        /// null if no definition exists. Observation labels always return null.
        /// </summary>
        public bool TryGetDefinition(MaterialPropertyLabel label, out MaterialPropertyDefinition definition)
            => m_Lookup.TryGetValue(label, out definition);

        public MaterialPropertyDefinition GetDefinition(MaterialPropertyLabel label)
        {
            m_Lookup.TryGetValue(label, out var def);
            return def;
        }

        public int Count => m_Definitions.Length;
    }
}

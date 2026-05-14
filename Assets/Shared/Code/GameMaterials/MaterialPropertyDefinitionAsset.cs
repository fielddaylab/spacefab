using FieldDay.Assets;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Global registry of MaterialPropertyDefinitions, keyed by
    /// MaterialPropertyLabel. Multiple definitions per label are supported -
    /// each represents a distinct dependency tree that can confirm the same
    /// property (e.g., a structural-evidence path and a behavioral-evidence
    /// path for PDopantFor). The evaluator OR-combines them: any one
    /// definition's dependency tree being satisfied confirms the property.
    /// Observation labels have no entry; they are leaves, not derived.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Game Materials/Property Definition Registry")]
    public class MaterialPropertyDefinitionAsset : GlobalAsset
    {
        private static readonly MaterialPropertyDefinition[] s_EmptyDefinitions = new MaterialPropertyDefinition[0];

        [SerializeField] private MaterialPropertyDefinition[] m_Definitions;

        private Dictionary<MaterialPropertyLabel, MaterialPropertyDefinition[]> m_Lookup;

        public override void Mount()
        {
            // 1. Bucket definitions by label so multiple authored definitions
            // for the same label end up in one array.
            Dictionary<MaterialPropertyLabel, List<MaterialPropertyDefinition>> bucket = new Dictionary<MaterialPropertyLabel, List<MaterialPropertyDefinition>>();
            for (int i = 0; i < m_Definitions.Length; i++)
            {
                MaterialPropertyDefinition def = m_Definitions[i];
                if (def == null) continue;
                if (!bucket.TryGetValue(def.Label, out var list))
                {
                    list = new List<MaterialPropertyDefinition>(1);
                    bucket[def.Label] = list;
                }
                list.Add(def);
            }

            // 2. Freeze each bucket into an array so callers don't have to
            // worry about list mutation.
            m_Lookup = new Dictionary<MaterialPropertyLabel, MaterialPropertyDefinition[]>(bucket.Count);
            foreach (var kvp in bucket)
            {
                m_Lookup[kvp.Key] = kvp.Value.ToArray();
            }
        }

        public override void Unmount() => m_Lookup = null;

        /// <summary>
        /// Returns every definition registered for the given persistent
        /// property label. Empty array if none. Observation labels always
        /// return empty. Iteration order matches the asset's m_Definitions
        /// array order, so callers that need determinism can rely on that
        /// (e.g., "the first satisfied definition wins" in the evaluator).
        /// </summary>
        public MaterialPropertyDefinition[] GetDefinitions(MaterialPropertyLabel label)
        {
            if (m_Lookup != null && m_Lookup.TryGetValue(label, out var defs))
            {
                return defs;
            }
            return s_EmptyDefinitions;
        }

        /// <summary>
        /// Returns the first registered definition for the label, or null if
        /// none. Prefer GetDefinitions for new callers - the evaluator needs
        /// the full set to support alternate-path confirmation.
        /// </summary>
        public bool TryGetDefinition(MaterialPropertyLabel label, out MaterialPropertyDefinition definition)
        {
            if (m_Lookup != null && m_Lookup.TryGetValue(label, out var defs) && defs.Length > 0)
            {
                definition = defs[0];
                return true;
            }
            definition = null;
            return false;
        }

        /// <summary>
        /// Returns the first registered definition for the label. Prefer
        /// GetDefinitions for new callers.
        /// </summary>
        public MaterialPropertyDefinition GetDefinition(MaterialPropertyLabel label)
        {
            TryGetDefinition(label, out var def);
            return def;
        }

        public int Count => m_Definitions.Length;
    }
}

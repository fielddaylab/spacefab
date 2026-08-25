using BeauUtil;
using FieldDay.Assets;
using ScriptableBake;
using SpaceFab.Materials;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using BeauUtil.Debugger;
using UnityEditor;
#endif // UNITY_EDITOR

namespace SpaceFab
{
    /// <summary>
    /// Defines the canonical ordering of all materials for save serialization.
    /// The array index of each entry is its bit position in the researched-materials bitmask.
    ///
    /// Also carries a baked snapshot of each material's discoverable properties, gathered from
    /// every MaterialAsset in the project rather than from whichever ones are mounted. The
    /// MaterialAsset pack only streams in for in-game scenes, so callers that need the
    /// fully-researched picture read it from here instead of walking the asset registry.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Game Materials/Material Order")]
    public class MaterialOrderAsset : GlobalAsset, IBaked
    {
        [AssetName(typeof(MaterialAsset))]
        [SerializeField] private StringHash32[] m_MaterialIds;

        // Parallel to m_MaterialIds: entry i holds every discoverable property authored on the
        // material at index i. Rebuilt at build time and by SpaceFab -> Materials -> Rebake
        // Material Knowledge.
        [SerializeField] private MaterialPropertyRecord[] m_BakedKnowledge;

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
        /// Falls back to a linear scan when the lookup hasn't been built, so the editor bake
        /// path can resolve indices against an unmounted asset.
        /// </summary>
        public bool TryGetIndex(StringHash32 materialId, out int index) {
            if (m_IndexLookup != null) {
                return m_IndexLookup.TryGetValue(materialId, out index);
            }

            for (int i = 0; i < m_MaterialIds.Length; i++) {
                if (m_MaterialIds[i] == materialId) {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        public StringHash32 GetId(int index) => m_MaterialIds[index];
        public int Count => m_MaterialIds.Length;

        /// <summary>
        /// True once the baked snapshot covers every material in the ordering. False means the
        /// bake has never run, or materials were added to the ordering afterwards.
        /// </summary>
        public bool HasBakedKnowledge => m_BakedKnowledge != null && m_BakedKnowledge.Length == m_MaterialIds.Length;

        /// <summary>
        /// Returns the baked discoverable-property set for the material at the given index.
        /// Empty when the snapshot doesn't cover that index.
        /// </summary>
        public MaterialPropertyRecord GetBakedKnowledge(int index) {
            if (m_BakedKnowledge == null || index < 0 || index >= m_BakedKnowledge.Length) {
                return default;
            }

            return m_BakedKnowledge[index];
        }

#if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            return BakeKnowledge();
        }

        // Editor entry point for rebaking without running a full build. Materials authored or
        // re-authored since the last bake stay out of the snapshot until this runs.
        [MenuItem("SpaceFab/Materials/Rebake Material Knowledge")]
        private static void RebakeMaterialKnowledge() {
            MaterialOrderAsset materialOrder = Baking.FindAsset<MaterialOrderAsset>();
            if (materialOrder == null) {
                Log.Error("[MaterialOrderAsset] No MaterialOrderAsset in the project to rebake");
                return;
            }

            if (!materialOrder.BakeKnowledge()) {
                Log.Msg("[MaterialOrderAsset] Material knowledge snapshot already up to date");
                return;
            }

            EditorUtility.SetDirty(materialOrder);
            AssetDatabase.SaveAssets();
            Log.Msg("[MaterialOrderAsset] Rebaked material knowledge for {0} materials", materialOrder.Count);
        }

        // Rebuilds the snapshot from every MaterialAsset in the project, mounted or not. A
        // material contributes only the properties authored on it - the rest aren't
        // discoverable and must never be confirmed. Dynamic labels (PDopantFor / NDopantFor)
        // are crossed with that material's authored Contexts, the same pairing the hypothesis
        // ground-truth check uses. Returns true if the snapshot actually changed.
        private bool BakeKnowledge() {
            MaterialPropertyRecord[] baked = new MaterialPropertyRecord[m_MaterialIds.Length];

            foreach (MaterialAsset material in Baking.FindAssets<MaterialAsset>()) {
                if (!TryGetIndex(material.AssetId, out int materialIdx)) {
                    // Authored but outside the canonical ordering: no save bit, nowhere to record.
                    continue;
                }

                if (material.Properties == null) {
                    continue;
                }

                for (int i = 0; i < material.Properties.Length; i++) {
                    MaterialPropertyLabel label = material.Properties[i];

                    // Observation-only labels are chamber evidence, never confirmed properties.
                    if (!MaterialPropertyLabelUtility.IsPersistent(label)) {
                        continue;
                    }

                    if (!MaterialPropertyLabelUtility.IsDynamic(label)) {
                        MaterialPropertyRecordUtility.TrySet(ref baked[materialIdx], label, StringHash32.Null, this);
                        continue;
                    }

                    if (material.Contexts == null || material.Contexts.Length == 0) {
                        Log.Warn("[MaterialOrderAsset] Material '{0}' authors dynamic property '{1}' but has no Contexts", material.name, label);
                        continue;
                    }

                    for (int c = 0; c < material.Contexts.Length; c++) {
                        MaterialAsset context = material.Contexts[c];
                        if (context == null) {
                            continue;
                        }

                        MaterialPropertyRecordUtility.TrySet(ref baked[materialIdx], label, context.AssetId, this);
                    }
                }
            }

            if (m_BakedKnowledge != null && m_BakedKnowledge.Length == baked.Length) {
                bool changed = false;
                for (int i = 0; i < baked.Length && !changed; i++) {
                    changed = !MaterialPropertyRecordUtility.AreEqual(m_BakedKnowledge[i], baked[i]);
                }

                if (!changed) {
                    return false;
                }
            }

            m_BakedKnowledge = baked;
            return true;
        }

#endif // UNITY_EDITOR
    }
}

using BeauUtil;
using FieldDay.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Sequence
{
    /// <summary>
    /// One reusable wafer-state image used as a building block for sequence cards. The id is
    /// authored by hand (kebab-case is the project convention) and referenced from
    /// SequenceStepEntry's ConvertFrom / ConvertToA / ConvertToB fields via the
    /// [WaferStepUIRef] dropdown.
    /// </summary>
    [Serializable]
    public struct WaferStepUIEntry
    {
        public SerializedHash32 Id;
        public Sprite Sprite;
    }

    /// <summary>
    /// Global asset holding the full palette of wafer-state images that sequence cards compose
    /// from. Accessed via Find.GlobalAsset&lt;WaferStepUILookup&gt;(). The companion
    /// [WaferStepUIRef] attribute drives a dropdown of these entries in the inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Fabrication/Wafer Step UI Lookup")]
    public class WaferStepUILookup : GlobalAsset
    {
        [SerializeField] private WaferStepUIEntry[] m_Entries;

        // Cached id -> sprite lookup. Rebuilt lazily on first access.
        private Dictionary<StringHash32, Sprite> m_SpritesByIdCache;

        // Exposed for the editor dropdown drawer to enumerate available ids. Not for runtime use.
        public IReadOnlyList<WaferStepUIEntry> Entries => m_Entries;

        // Returns the sprite for the given id, or null if the id is empty or not found.
        public Sprite GetSprite(SerializedHash32 id)
        {
            if (id.IsEmpty) {
                return null;
            }
            EnsureCache();
            m_SpritesByIdCache.TryGetValue(id.Hash(), out Sprite sprite);
            return sprite;
        }

        // True when the id is non-empty and present in the lookup. Empty hashes always return false.
        public bool HasEntry(SerializedHash32 id)
        {
            if (id.IsEmpty) {
                return false;
            }
            EnsureCache();
            return m_SpritesByIdCache.ContainsKey(id.Hash());
        }

        // Builds the id -> sprite cache from m_Entries on first lookup. Idempotent.
        private void EnsureCache()
        {
            if (m_SpritesByIdCache != null) {
                return;
            }
            int count = m_Entries != null ? m_Entries.Length : 0;
            m_SpritesByIdCache = new Dictionary<StringHash32, Sprite>(count, CompareUtils.DefaultEquals<StringHash32>());
            for (int i = 0; i < count; i++) {
                WaferStepUIEntry entry = m_Entries[i];
                if (entry.Id.IsEmpty) {
                    continue;
                }
                m_SpritesByIdCache[entry.Id.Hash()] = entry.Sprite;
            }
        }
    }
}

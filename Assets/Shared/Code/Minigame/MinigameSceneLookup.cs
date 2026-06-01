using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using SpaceFab.Overarching;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    /// <summary>
    /// One MinigameId → scene mapping authored on MinigameSceneLookup.
    /// </summary>
    [Serializable]
    public struct MinigameSceneEntry
    {
        public MinigameId Minigame;
        public SceneReference Scene;
    }

    /// <summary>
    /// Global lookup from MinigameId to the minigame's main scene. Lets code inside a minigame
    /// scene resolve its own (or another minigame's) scene without an overarching MinigameZone —
    /// e.g. the Design results "Continue" flow reloading the Design scene for the next level.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Minigame Scene Lookup")]
    public class MinigameSceneLookup : GlobalAsset
    {
        [SerializeField] private MinigameSceneEntry[] m_Entries;

        private Dictionary<MinigameId, SceneReference> m_SceneLookup;

        public override void Mount()
        {
            m_SceneLookup = new Dictionary<MinigameId, SceneReference>(m_Entries.Length);
            for (int i = 0; i < m_Entries.Length; i++)
            {
                m_SceneLookup[m_Entries[i].Minigame] = m_Entries[i].Scene;
            }
        }

        public override void Unmount()
        {
            m_SceneLookup = null;
        }

        // Resolves the scene for the given minigame, or false if no entry was authored for it.
        public bool TryGetScene(MinigameId minigame, out SceneReference scene)
        {
            return m_SceneLookup.TryGetValue(minigame, out scene);
        }
    }
}

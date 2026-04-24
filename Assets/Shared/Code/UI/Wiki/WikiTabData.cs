using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// ScriptableObject authored per wiki section/tab. Identity (used for external OpenTo(tabId)
    /// calls) is the asset's name via NamedAsset.AssetId. Carries an ordered list of pages; a
    /// tab is considered "unlocked" iff at least one of its pages is unlocked in PlayerProgress.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Wiki/Tab")]
    public class WikiTabData : NamedAsset {
        [SerializeField] private string m_Title;
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private WikiPageData[] m_Pages;

        public string Title { get { return m_Title; } }
        public Sprite Icon { get { return m_Icon; } }
        public WikiPageData[] Pages { get { return m_Pages; } }
    }
}

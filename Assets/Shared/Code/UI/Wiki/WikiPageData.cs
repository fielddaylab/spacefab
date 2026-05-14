using FieldDay.Assets;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// ScriptableObject authored per wiki page. The page's identity (used for unlock tracking
    /// and external OpenTo(pageId) calls) is the asset's name, exposed via the NamedAsset.AssetId
    /// base property.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Wiki/Page")]
    public class WikiPageData : NamedAsset {
        [SerializeField] private string m_Title;
        // Small sprite rendered on the page's paginator thumbnail (one slot in the horizontal
        // scroll strip). Distinct from the large Illustration shown in the page body.
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private Sprite m_Illustration;
        [SerializeField, TextArea(3, 20)] private string m_Body;

        public string Title { get { return m_Title; } }
        public Sprite Icon { get { return m_Icon; } }
        public Sprite Illustration { get { return m_Illustration; } }
        public string Body { get { return m_Body; } }
    }
}

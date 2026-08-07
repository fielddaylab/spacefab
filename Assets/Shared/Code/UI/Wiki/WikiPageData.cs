using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Materials;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// ScriptableObject authored per wiki page. The page's identity (used for unlock tracking
    /// and external OpenTo(pageId) calls) is the asset's name, exposed via the NamedAsset.AssetId
    /// base property.
    ///
    /// Two page kinds, discriminated by m_MaterialId:
    ///   - Default page (m_MaterialId is empty): renders title + illustration + body text. The
    ///     WikiPageContentWidgets.DefaultGroup wrapper is enabled.
    ///   - Material page (m_MaterialId is set): renders title + a chip column of the player's
    ///     confirmed persistent properties on the named material. Illustration comes from the
    ///     material's ResearchMaterialView rather than m_IllustrationFrames. The
    ///     WikiPageContentWidgets.MaterialCharacteristicsGroup wrapper is enabled.
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceFab/Wiki/Page")]
    public class WikiPageData : NamedAsset {
        [SerializeField] private bool m_isPlanet;
        [SerializeField] private string m_Title;
        // Small sprite rendered on the page's paginator thumbnail (one slot in the horizontal
        // scroll strip). Distinct from the large Illustration shown in the page body.
        [SerializeField] private Sprite m_Icon;
        // Large illustration shown in the page body, cycled in authoring order. One frame is a
        // still image; empty hides the slot. 
        [SerializeField] private Sprite[] m_IllustrationFrames;
        // Cycle rate for m_IllustrationFrames. Ignored by pages authoring fewer than two frames.
        [SerializeField] private float m_IllustrationFPS = 0.5f;
        [SerializeField, TextArea(3, 20)] private string m_Body;

        // Optional. When set, this page is a "material page" — its body is replaced by a
        // chip column of confirmed persistent properties on this material, and the
        // illustration is sourced from the material's ResearchMaterialView rather than
        // m_IllustrationFrames. Default (empty) yields the standard title + illustration + body
        // shape.
        [AssetName(typeof(MaterialAsset))]
        [SerializeField] private StringHash32 m_MaterialId;

        public bool isPlanet { get { return m_isPlanet; } }
        public string Title { get { return m_Title; } }
        public Sprite Icon { get { return m_Icon; } }
        public Sprite[] IllustrationFrames { get { return m_IllustrationFrames; } }
        public float IllustrationFPS { get { return m_IllustrationFPS; } }
        public string Body { get { return m_Body; } }
        public StringHash32 MaterialId { get { return m_MaterialId; } }
        public bool IsMaterialPage { get { return !m_MaterialId.IsEmpty; } }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    /// <summary>
    /// Pure-data MonoBehaviour on the wiki prefab's PageArea. Holds inspector references to
    /// the widgets that display the currently-selected WikiPageData. Consumed by
    /// WikiVisualsUpdateSystem, which pushes page fields into these widgets each frame while
    /// the panel is expanded.
    ///
    /// Two content shapes:
    ///   - Default page: DefaultGroup wraps the body text; MaterialCharacteristicsGroup is
    ///     disabled. Title + IllustrationImage render as authored on WikiPageData.
    ///   - Material page: MaterialCharacteristicsGroup wraps the chip column populated by
    ///     WikiCharacteristicsLoadUtility; DefaultGroup is disabled. IllustrationImage's
    ///     sprite is sourced from the material's ResearchMaterialView rather than
    ///     WikiPageData.Illustration. Title still renders as authored.
    ///
    /// No logic here intentionally — display-side equivalent of a pure-data component. The
    /// write path lives in WikiVisualsUpdateSystem (toggle + content bind) and
    /// WikiCharacteristicsLoadUtility (chip alloc + layout).
    /// </summary>
    public class WikiPageContentWidgets : MonoBehaviour {
        public TextMeshProUGUI TitleText;
        public Image IllustrationImage;

        // Default-page body wrapper. WikiVisualsUpdateSystem enables
        // this when the active page's MaterialId is empty. The body
        // text writes into BodyText below.
        public GameObject DefaultGroup;
        public TextMeshProUGUI BodyText;

        // Material-page chip column wrapper. Enabled when MaterialId
        // is set. WikiCharacteristicsLoadUtility pool-allocs chips
        // under CharacteristicsContainer and resizes this group's
        // RectTransform to fit them.
        public GameObject MaterialCharacteristicsGroup;
        public RectTransform CharacteristicsContainer;
    }
}

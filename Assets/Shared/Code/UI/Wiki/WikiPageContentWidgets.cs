using TMPro;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Inspector references to the widgets that display the selected WikiPageData, authored on the
    /// wiki prefab's PageArea. Every field is required authoring; WikiVisualsUtility asserts on
    /// each rather than skipping the ones that aren't wired up.
    ///
    /// Title and Illustration render on both page kinds. The two groups are mutually
    /// exclusive: default pages show DefaultGroup's body text, material pages show the
    /// characteristics chip column and take their illustration from the material asset — a single
    /// still frame — instead of WikiPageData's authored frame sequence.
    ///
    /// Data only — the write path is WikiVisualsUtility for the group toggle and content bind, and
    /// WikiCharacteristicsLoadUtility for the chips.
    /// </summary>
    public class WikiPageContentWidgets : MonoBehaviour {
        public TextMeshProUGUI TitleText;
        // Wraps the illustration Image so a page can author an animated sequence. Still pages bind
        // through it too, as a one-frame cycle.
        public SpriteCycler Illustration;

        public GameObject DefaultGroup;
        public TextMeshProUGUI BodyText;

        // Chips are pool-allocated under CharacteristicsContainer, and the group's RectTransform
        // is resized to fit them.
        public GameObject MaterialCharacteristicsGroup;
        public RectTransform CharacteristicsContainer;
        public GameObject PlanetDetailsContainer;
    }
}

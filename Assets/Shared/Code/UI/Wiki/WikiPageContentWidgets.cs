using SpaceFab.Research;
using TMPro;
using UnityEngine;

namespace SpaceFab.UI {
    /// <summary>
    /// Data only — the write path is WikiVisualsUtility for the group toggle and content bind, and
    /// WikiCharacteristicsLoadUtility / WikiObservationLoadUtility / WikiPropertyLoadUtility for
    /// the chips.
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

        [Header("Observation Page")]
        public GameObject ObservationGroup;
        // Caption above the chip column. Bound from the page's Body when it authors one.
        public RectTransform ObservationChipContainer;

        [Header("Property Page")]
        public GameObject PropertyGroup;
        // Authored on the prefab rather than pool-allocated — there is exactly one per page, and
        // it carries the property-selection click.
        public ResearchObservationChip PropertyChip;
        public TextMeshProUGUI PropertyBodyText;
        public RectTransform PropertyLeafChipContainer;
    }
}

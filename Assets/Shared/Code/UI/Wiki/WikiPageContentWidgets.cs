using FieldDay;
using FieldDay.UI;
using SpaceFab.Research;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        public TextMeshProUGUI BodyText;

        [Header("Material Page")]
        public GameObject MaterialGroup;
        public Image MaterialIcon;
        public TextMeshProUGUI MaterialLabel;
        public LayoutSizeGroup MaterialLabelSize;

        [Header("Property Chips")]
        public ResearchObservationChip PropertyChip;
        public ResearchObservationChip[] ObservationChips;

        [Header("Sizing")]
        public LayoutOptions VerticalLayout = LayoutOptions.PreferredSize(4, 1);
    }
}

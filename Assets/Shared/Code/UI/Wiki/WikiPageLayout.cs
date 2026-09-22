using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.UI;
using SpaceFab.Comic;
using SpaceFab.Materials;
using SpaceFab.Research;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public class WikiPageLayout : MonoBehaviour {
        public TextMeshProUGUI TitleText;
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

        [Header("Vertical Layout")]
        public RectTransform LayoutRoot;
        public LayoutOptions VerticalLayout = LayoutOptions.PreferredSize(4, 1);
    }

    static public partial class WikiUtility {
        static public void PopulatePageContent(WikiPageLayout layout, WikiPageData pageData, IWikiContentFilter contentFilter) {
            ClearPageContent(layout);

            if (pageData.IsMaterialPage) {
                PopulateMaterialPage(layout, pageData, contentFilter);
            } else if (pageData.IsObservationPage) {
                PopulateObservationsPage(layout, pageData);
            } else if (pageData.IsPropertyPage) {
                PopulatePropertyPage(layout, pageData);
            } else {
                PopulateDefaultPageContent(layout, pageData);
            }

            if (layout.TitleText.gameObject.activeSelf) {
                Positioning.ResizeToPreferred(layout.TitleText);
            }

            if (layout.BodyText.gameObject.activeSelf) {
                Positioning.ResizeToPreferred(layout.BodyText);
            }

            using (var query = Positioning.QueryLayoutChildren(layout.LayoutRoot)) {
                Positioning.VerticalLayout(query, layout.VerticalLayout);
            }

            layout.LayoutRoot.gameObject.SetActive(true);
        }

        static private void PopulateMaterialPage(WikiPageLayout layout, WikiPageData pageData, IWikiContentFilter contentFilter) {
            SetPageMaterialIconActive(layout, true);

            MaterialAsset materialData = Find.NamedAsset<MaterialAsset>(pageData.MaterialId);
            layout.TitleText.SetText(materialData.DisplayName);
        }

        static private void PopulateObservationsPage(WikiPageLayout layout, WikiPageData pageData) {
            SetPageMaterialIconActive(layout, false);
        }

        static private void PopulatePropertyPage(WikiPageLayout layout, WikiPageData pageData) {
            SetPageMaterialIconActive(layout, false);

            MaterialPropertyCheck prop = pageData.PropertyCheck;
        }

        static private void PopulateDefaultPageContent(WikiPageLayout layout, WikiPageData pageData) {
            layout.TitleText.gameObject.SetActive(true);
        }

        static private void SetPageMaterialIconActive(WikiPageLayout layout, bool active) {
            layout.MaterialGroup.SetActive(active);

            Vector4 margins = layout.TitleText.margin;
            margins.z = active ? 42 : 0;
            layout.TitleText.margin = margins;
        }

        static public void ClearPageContent(WikiPageLayout layout) {
            layout.LayoutRoot.gameObject.SetActive(false);

            foreach(var chip in layout.ObservationChips) {
                chip.Tag.SetId(null);
            }

            layout.PropertyChip.Tag.SetId(null);
        }
    }
}

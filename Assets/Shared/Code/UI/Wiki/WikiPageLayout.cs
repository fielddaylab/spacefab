using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.UI;
using SpaceFab.Comic;
using SpaceFab.Materials;
using SpaceFab.Research;
using System;
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

        [NonSerialized] public WikiPageType CurrentPageType;
        [NonSerialized] public StringHash32 CurrentMaterialId;
        [NonSerialized] public MaterialPropertyLabel CurrentPropertyChip;
        [NonSerialized] public int CurrentObservationChipCount;
        [NonSerialized] public WikiPageChipState[] CurrentObservationChips;

        private void Awake() {
            CurrentObservationChips = new WikiPageChipState[ObservationChips.Length];
            CurrentObservationChipCount = 0;
            CurrentPageType = WikiPageType.Default;
        }
    }

    public enum WikiPageType : byte {
        Default,
        Material,
        Property,
        Observations
    }

    public struct WikiPageChipState {
        public MaterialPropertyLabel PropertyChip;
    }

    static public partial class WikiLayoutUtility {
        #region Population

        /// <summary>
        /// Populates the page layout with the content within the given page data.
        /// </summary>
        static public void PopulatePageContent(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            ClearPageContent(layout);

            if (pageData.IsMaterialPage) {
                PopulateMaterialPage(layout, pageData, content);
            } else if (pageData.IsObservationPage) {
                PopulateObservationsPage(layout, pageData, content);
            } else if (pageData.IsPropertyPage) {
                PopulatePropertyPage(layout, pageData, content);
            } else {
                PopulateDefaultPageContent(layout, pageData, content);
            }

            if (layout.TitleText.gameObject.activeSelf) {
                Positioning.ResizeToPreferred(layout.TitleText);
            }

            if (layout.BodyText.gameObject.activeSelf) {
                Positioning.ResizeToPreferred(layout.BodyText);
            }

            if (layout.MaterialGroup.activeSelf) {
                Positioning.ResizeToPreferred(layout.MaterialLabel);
                layout.MaterialLabelSize.Sync();
            }

            using (var query = Positioning.QueryLayoutChildren(layout.LayoutRoot)) {
                Positioning.VerticalLayout(query, layout.VerticalLayout);
            }

            layout.LayoutRoot.gameObject.SetActive(true);
        }

        static private void PopulateMaterialPage(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Material;
            layout.CurrentMaterialId = pageData.MaterialId;
            SetPageMaterialIconActive(layout, true);

            MaterialAsset materialData = Find.NamedAsset<MaterialAsset>(pageData.MaterialId);
            layout.MaterialIcon.sprite = materialData.GemSprite;
            layout.MaterialLabel.SetText(materialData.ShortName);

            layout.TitleText.SetText(materialData.DisplayName);
            layout.BodyText.SetTextAndActive(pageData.BodyText);
        }

        static private void PopulateObservationsPage(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Observations;
            SetPageMaterialIconActive(layout, false);
            Assert.True(content.ResearchContext.Present, "Observation page should not be viewed outside of Research");

            layout.TitleText.SetText(pageData.Title);
            layout.BodyText.SetTextAndActive(pageData.BodyText);

        }

        static private void PopulatePropertyPage(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Property;
            SetPageMaterialIconActive(layout, false);
            Assert.True(content.ResearchContext.Present, "Property page should not be viewed outside of Research");

            MaterialPropertyCheck prop = pageData.PropertyCheck;
        }

        static private void PopulateDefaultPageContent(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Default;
            SetPageMaterialIconActive(layout, false);

            layout.TitleText.SetText(pageData.Title);
            layout.BodyText.SetTextAndActive(pageData.BodyText);
            SetIllustrations(layout, pageData);
        }

        static private void SetPageMaterialIconActive(WikiPageLayout layout, bool active) {
            layout.MaterialGroup.SetActive(active);

            layout.TitleText.maxLineWidth = active ? 200 : 240;
        }

        static private void SetIllustrations(WikiPageLayout layout, WikiPageData pageData) {
            if (pageData.IllustrationFrames.Length > 0) {
                layout.Illustration.gameObject.SetActive(true);
                SpriteCyclerUtility.SetFrames(layout.Illustration, pageData.IllustrationFrames, pageData.IllustrationFPS);
            }
        }

        /// <summary>
        /// Clears the page layout.
        /// </summary>
        static public void ClearPageContent(WikiPageLayout layout) {
            layout.LayoutRoot.gameObject.SetActive(false);

            foreach(var chip in layout.ObservationChips) {
                chip.Tag.SetId(null);
                chip.gameObject.SetActive(false);
            }

            layout.PropertyChip.Tag.SetId(null);
            layout.PropertyChip.gameObject.SetActive(false);

            layout.BodyText.gameObject.SetActive(false);
            layout.Illustration.gameObject.SetActive(false);

            layout.CurrentPageType = WikiPageType.Default;
            layout.CurrentPropertyChip = default;
            layout.CurrentObservationChipCount = 0;
            layout.CurrentMaterialId = default;
        }

        #endregion // Population

        #region Research Updates

        static public void UpdateMaterialPageData(WikiPageLayout pageLayout, WikiContent content) {

        }

        static public void UpdateObservationPageData(WikiPageLayout pageLayout, WikiContent content) {

        }

        static public void UpdatePropertyPageData(WikiPageLayout pageLayout, WikiContent content) {

        }

        #endregion // Research Updates

        #region Handlers

        #endregion // Handlers
    }
}

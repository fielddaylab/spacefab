using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Animation;
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
        public TextMeshProUGUI CaptionText;
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

        [Header("Masking")]
        public Mask AnimatedMask;

        [NonSerialized] public WikiPageType CurrentPageType;
        [NonSerialized] public StringHash32 CurrentMaterialId;
        [NonSerialized] public MaterialPropertyLabel CurrentPropertyChip;
        [NonSerialized] public int CurrentObservationChipCount;
        [NonSerialized] public WikiPageChipState[] CurrentObservationChipData;

        [NonSerialized] public AnimHandle AppearAnim;

        [NonSerialized] public float OriginalIllustrationHeight;

        private void Awake() {
            CurrentObservationChipData = new WikiPageChipState[ObservationChips.Length];
            CurrentObservationChipCount = 0;
            CurrentPageType = WikiPageType.Default;

            AnimatedMask.enabled = false;
            AnimatedMask.graphic.enabled = false;

            OriginalIllustrationHeight = Illustration.Target.rectTransform.sizeDelta.y;
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
        public sbyte ContextIndex;
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

            if (layout.CaptionText.gameObject.activeSelf) {
                Positioning.ResizeToPreferred(layout.CaptionText);
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
            Anims.Replace(ref layout.AppearAnim, DissolveInAnimInstance, layout, 0);
        }

        static private unsafe void PopulateMaterialPage(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Material;
            layout.CurrentMaterialId = pageData.MaterialId;
            SetPageMaterialIconActive(layout, true);

            MaterialAsset materialData = Find.NamedAsset<MaterialAsset>(pageData.MaterialId);
            layout.MaterialIcon.sprite = materialData.GemSprite;
            layout.MaterialLabel.SetText(materialData.ShortName);

            layout.TitleText.SetText(materialData.DisplayName);
            layout.BodyText.SetTextAndActive(pageData.BodyText);
            layout.CaptionText.SetTextAndActive(pageData.CaptionText);

            Find.State(out PlayerProgressState playerProgress);
            Find.GlobalAsset(out MaterialOrderAsset materialOrder);

            int allocatedProps = 0;
            bool isNDopant = false,
                isPDopant = false;

            // build static properties first
            for(int i = 0; i < materialData.Properties.Length; i++) {
                MaterialPropertyLabel propLabel = materialData.Properties[i];
                if (!MaterialPropertyLabelUtility.IsPersistent(propLabel)) {
                    continue;
                }
                if (WikiContentUtility.IsMaterialPropertyExcluded(propLabel, playerProgress)) {
                    continue;
                }

                if (propLabel == MaterialPropertyLabel.NDopantFor) {
                    isNDopant = true;
                    continue;
                }
                if (propLabel == MaterialPropertyLabel.PDopantFor) {
                    isPDopant = true;
                    continue;
                }

                ResearchObservationChip chip = layout.ObservationChips[allocatedProps];
                layout.CurrentObservationChipData[allocatedProps] = new WikiPageChipState() {
                    PropertyChip = propLabel,
                    ContextIndex = -1
                };

                chip.gameObject.SetActive(true);

                allocatedProps++;
            }

            Assert.False(isPDopant & isNDopant, "Material is both P and N dopants, which is not supported");

            // dopant properties second
            if (isPDopant | isNDopant) {

                int* contextIndices = stackalloc int[16];
                int contextCount = materialData.Contexts.Length;

                for (int i = 0; i < materialData.Contexts.Length; i++) {
                    materialOrder.TryGetIndex(materialData.Contexts[i].AssetId, out int index);
                    contextIndices[i] = index;
                }

                // sort indices so we have a consistent ordering in the wiki
                Unsafe.Quicksort(contextIndices, contextCount);

                for(int i = 0; i < contextCount; i++) {
                    ResearchObservationChip chip = layout.ObservationChips[allocatedProps];
                    layout.CurrentObservationChipData[allocatedProps] = new WikiPageChipState() {
                        PropertyChip = isPDopant ? MaterialPropertyLabel.PDopantFor : MaterialPropertyLabel.NDopantFor,
                        ContextIndex = (sbyte) contextIndices[i]
                    };

                    chip.gameObject.SetActive(true);
                    allocatedProps++;
                }
            }

            layout.CurrentObservationChipCount = allocatedProps;
        }

        static private void PopulateObservationsPage(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Observations;
            SetPageMaterialIconActive(layout, false);
            Assert.True(content.ResearchContext.Present, "Observation page should not be viewed outside of Research");

            layout.TitleText.SetText(pageData.Title);
            layout.BodyText.SetTextAndActive(pageData.BodyText);
            layout.CaptionText.SetTextAndActive(pageData.CaptionText);
        }

        static private void PopulatePropertyPage(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Property;
            SetPageMaterialIconActive(layout, false);
            Assert.True(content.ResearchContext.Present, "Property page should not be viewed outside of Research");

            layout.TitleText.SetText(pageData.Title);
            layout.BodyText.SetTextAndActive(pageData.BodyText);
            layout.CaptionText.SetTextAndActive(pageData.CaptionText);

            MaterialPropertyCheck prop = pageData.PropertyCheck;
            layout.CurrentPropertyChip = prop.Label;

            layout.PropertyChip.gameObject.SetActive(true);
            layout.PropertyChip.SetState(MaterialPropertyLabelDisplay.GetPropertyName(prop.Label), ChipFillState.Filled, false, MaterialObservationChamberLookup.GetChamberType(prop.Label));
        }

        static private void PopulateDefaultPageContent(WikiPageLayout layout, WikiPageData pageData, WikiContent content) {
            layout.CurrentPageType = WikiPageType.Default;
            SetPageMaterialIconActive(layout, false);

            layout.TitleText.SetText(pageData.Title);
            layout.BodyText.SetTextAndActive(pageData.BodyText);
            layout.CaptionText.SetTextAndActive(pageData.CaptionText);
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
                Positioning.SetHeightDelta(layout.Illustration.Target.rectTransform, layout.OriginalIllustrationHeight * pageData.IllustrationImageScale);
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
            layout.CaptionText.gameObject.SetActive(false);

            layout.CurrentPageType = WikiPageType.Default;
            layout.CurrentPropertyChip = default;
            layout.CurrentObservationChipCount = 0;
            layout.CurrentMaterialId = default;

            layout.AnimatedMask.enabled = false;
            layout.AnimatedMask.graphic.enabled = false;
            Anims.Cancel(ref layout.AppearAnim);
        }

        /// <summary>
        /// Clears the page layout, and any additional asset references.
        /// </summary>
        static public void WipePageContentCompletely(WikiPageLayout layout) {
            ClearPageContent(layout);
            layout.Illustration.Frames = Array.Empty<Sprite>();
            layout.Illustration.Target.sprite = null;
        }

        #endregion // Population

        #region Research Updates

        static public void UpdateMaterialPageData(WikiPageLayout pageLayout, WikiContent content) {
            if (pageLayout.CurrentPageType != WikiPageType.Material) {
                return;
            }

            Find.State(out PlayerProgressState playerProgress);
            Find.GlobalAsset(out MaterialOrderAsset materialOrder);

            MaterialPropertyRecord record = WikiContentUtility.GetMaterialRecord(pageLayout.CurrentMaterialId, playerProgress, content.ResearchContext);

            for(int i = 0; i < pageLayout.CurrentObservationChipCount; i++) {
                pageLayout.ObservationChips[i].Tag.SetId(null);
            }

            int usedChips = 0;
            for(int i = 0; i < pageLayout.CurrentObservationChipCount; i++) {
                WikiPageChipState chipData = pageLayout.CurrentObservationChipData[i];
                if (chipData.ContextIndex >= 0) {
                    int recordMask = chipData.PropertyChip == MaterialPropertyLabel.PDopantFor ? record.DynamicMask_PDopant : record.DynamicMask_NDopant;
                    int bitMask = 1 << chipData.ContextIndex;
                    if ((recordMask & bitMask) == bitMask) {
                        ResearchObservationChip chip = pageLayout.ObservationChips[usedChips++];
                        chip.SetState(MaterialPropertyLabelDisplay.GetPropertyName(chipData.PropertyChip), ChipFillState.Confirmed, false, ObservationType.ConfirmedProperty);
                        chip.Tag.SetId(WikiElementTagUtility.MaterialCharacteristicId(pageLayout.CurrentMaterialId, chipData.PropertyChip, materialOrder.GetId(chipData.ContextIndex)));
                    }
                } else {
                    int bitIndex = MaterialPropertyLabelUtility.GetStaticBitIndex(chipData.PropertyChip);
                    int bitMask = 1 << bitIndex;
                    if ((record.StaticMask & bitMask) == bitMask) {
                        ResearchObservationChip chip = pageLayout.ObservationChips[usedChips++];
                        chip.SetState(MaterialPropertyLabelDisplay.GetPropertyName(chipData.PropertyChip), ChipFillState.Confirmed, false, ObservationType.ConfirmedProperty);
                        chip.Tag.SetId(WikiElementTagUtility.MaterialCharacteristicId(pageLayout.CurrentMaterialId, chipData.PropertyChip));
                    }
                }
            }

            for(int i = usedChips; i < pageLayout.CurrentObservationChipCount; i++) {
                ResearchObservationChip chip = pageLayout.ObservationChips[i];
                chip.SetState("???", ChipFillState.Empty, false, ObservationType.ConfirmedProperty, false);
            }
        }

        static public void UpdateObservationPageData(WikiPageLayout pageLayout, WikiContent content) {
            if (pageLayout.CurrentPageType != WikiPageType.Observations) {
                return;
            }
        }

        static public void UpdatePropertyPageData(WikiPageLayout pageLayout, WikiContent content) {
            if (pageLayout.CurrentPageType != WikiPageType.Property) {
                return;
            }

            bool isActiveHypothesis = content.ResearchContext.Present
                && content.ResearchContext.ViewModel.HypothesisSelected
                && content.ResearchContext.ViewModel.HypothesisLabel == pageLayout.CurrentPropertyChip;

            pageLayout.PropertyChip.SetPickerChipDisabledVisual(isActiveHypothesis);
        }

        #endregion // Research Updates

        #region Animation

        static private readonly DissolveInAnimation DissolveInAnimInstance = new DissolveInAnimation();

        private sealed class DissolveInAnimation : LiteAnimator<WikiPageLayout> {
            public override void InitAnimation(WikiPageLayout target, ref LiteAnimatorState state) {
                state.ResetTime(0.44f);
                target.AnimatedMask.enabled = true;
                target.AnimatedMask.graphic.SetAlpha(0);
                target.AnimatedMask.graphic.enabled = true;
            }

            public override void ResetAnimation(WikiPageLayout target, ref LiteAnimatorState state) {
            }

            public override void UpdateAnimation(WikiPageLayout target, ref LiteAnimatorState state, float deltaTime) {
                if (state.IsLastFrame()) {
                    target.AnimatedMask.enabled = false;
                    target.AnimatedMask.graphic.enabled = false;
                } else {
                    target.AnimatedMask.graphic.SetAlpha(state.PercentProgress);
                }
            }
        }

        #endregion // Animation

        #region Handlers

        #endregion // Handlers
    }
}

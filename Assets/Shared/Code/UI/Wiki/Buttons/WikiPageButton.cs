using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using SpaceFab.Materials;
using SpaceFab.Onboarding;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public class WikiPageButton : GuiButton.Style {
        public Graphic Background;
        public Graphic Outline;
        public ElementTag Taggable;

        [Header("Material Page")]
        public GameObject MaterialLabelGroup;
        public TextMeshProUGUI MaterialLabel;
        public LayoutSizeGroup MaterialLabelSizer;

        public override void OnClick(GuiButton button) {
            
        }

        public override void UpdateInteractionState(GuiWidgetInteractableState state, GuiWidget source, GuiWidgetUpdateFlags flags) {
            
        }
    }

    static public partial class WikiLayoutUtility {
        static public void PopulatePageButton(WikiPageButton button, WikiPageData pageData, int pageIndex) {
            button.Widget.SetVariantValue(pageIndex);
            button.Widget.CursorHint.MarkDirty();

            Sprite pageIcon = pageData.Icon;

            if (pageData.IsMaterialPage) {
                MaterialAsset material = Find.NamedAsset<MaterialAsset>(pageData.MaterialId);
                button.MaterialLabel.SetText(material.ShortName);
                Positioning.ResizeToPreferred(button.MaterialLabel);
                button.MaterialLabelSizer.Sync();
                button.MaterialLabelGroup.SetActive(true);
                pageIcon = material.GemSprite;
                button.Widget.CursorHint.TooltipHeader = material.DisplayName;
                button.Taggable.SetId(WikiElementTagUtility.PageThumbId(material.ShortName));
            } else {
                button.MaterialLabelGroup.SetActive(false);
                button.Widget.CursorHint.TooltipHeader = pageData.Title;
                button.Taggable.SetId(WikiElementTagUtility.PageThumbId(pageData.Title));
            }

            button.Widget.ImageGraphic.sprite = pageIcon;
        }
    }
}
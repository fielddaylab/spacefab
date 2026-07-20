using System;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    public sealed class ShipListRow : GuiWidget {
        public Image SpeedIcon;
        public Image ShipIcon;
        public Image[] ShipBody;
        public RectTransform[] Slots;
        public Image[] SlotMaterials;
        public bool IsWideRow;

        [NonSerialized] public int ShipIndex;
    }

    static public partial class SupplyChainUtility {
        static public void PopulateShipInformation(ShipListRow row, SupplyShipAsset shipAsset, ShipListPanel panel) {
            row.ShipIcon.sprite = shipAsset.Icon;
            row.ShipIcon.color = shipAsset.IconColor;
            foreach(var bodySprite in row.ShipBody) {
                bodySprite.sprite = shipAsset.BodyImage;
            }

            ShipListPanel.SpeedIconConfig speedIcon = panel.SpeedIcons[shipAsset.Speed - 1];
            row.SpeedIcon.sprite = speedIcon.Image;
            row.SpeedIcon.rectTransform.sizeDelta = speedIcon.Size;

            for (int i = 0; i < row.Slots.Length; i++) {
                row.Slots[i].gameObject.SetActive(i < shipAsset.Capacity);
            }

            for (int i = 0; i < row.SlotMaterials.Length; i++) {
                row.SlotMaterials[i].enabled = false;
            }

            row.CursorHint.TooltipHeader = shipAsset.DisplayName;
        }
    }
}
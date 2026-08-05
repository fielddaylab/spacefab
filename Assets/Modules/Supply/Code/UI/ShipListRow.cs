using System;
using BeauRoutine;
using FieldDay;
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

        [NonSerialized] public int ShipIndex;
        [NonSerialized] public Vector2 TargetPos;
    }

    static public partial class SupplyChainUtility {
        static public void PopulateShipInformation(ShipListRow row, SupplyShipAsset shipAsset, ShipListPanel panel) {
            bool isWide = shipAsset.Capacity > 2;
            float iconPos = 95;
            float bodySize = 130;

            if (isWide) {
                iconPos += 20;
                bodySize += 20;
            }

            Positioning.SetOffsetX(row.ShipIcon.rectTransform, iconPos);
            
            row.ShipIcon.sprite = shipAsset.Icon;
            row.ShipIcon.color = shipAsset.IconColor;
            foreach(var bodySprite in row.ShipBody) {
                Positioning.SetWidthDelta(bodySprite.rectTransform, bodySize);
                bodySprite.sprite = shipAsset.BodyImage;
            }

            ShipListPanel.SpeedIconConfig speedIcon = panel.SpeedIcons[shipAsset.Speed];
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
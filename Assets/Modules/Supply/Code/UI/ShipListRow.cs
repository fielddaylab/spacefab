using System;
using BeauRoutine;
using FieldDay;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using SpaceFab.Fabrication;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    public sealed class ShipListRow : GuiWidget {
        public Image SpeedIcon;
        public Image ShipIcon;
        public Image[] ShipBody;
        public RectTransform[] Slots;
        public Image[] SlotMaterials;
        public TMP_Text ShipName;

        [Header("Links")]
        public Image LineLayer;
        public SupplyShipBreakdownRow StatsLayer;
        public LayoutStyleInfo Style;

        [NonSerialized] public int ShipIndex;
        [NonSerialized] public Vector2 TargetPos;

        protected override void OnDisable() {
            LineLayer.gameObject.SetActive(false);
            StatsLayer.gameObject.SetActive(false);
            
            base.OnDisable();
        }
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

            row.LineLayer.color = shipAsset.LineColor;

            row.ShipName.SetText(shipAsset.DisplayName);
            row.ShipName.color = shipAsset.Colors.Content;
        }

        static public void SyncShipRowPositions(ShipListRow row) {
            Vector2 anchorPos = row.Rect.anchoredPosition;
            row.LineLayer.rectTransform.anchoredPosition = anchorPos;
            Positioning.SetOffsetY(row.StatsLayer.Rect, anchorPos.y);
        }

        static public void SetShipRowStatsActive(ShipListRow row, bool active) {
            if (active) {
                row.StatsLayer.gameObject.SetActive(true);
                row.Style.Style.MarginLower.y = 52;
            } else {
                row.Style.Style.MarginLower.y = 0;
                row.StatsLayer.gameObject.SetActive(false);
            }
        }
    }
}
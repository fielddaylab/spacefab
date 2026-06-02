using System;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    public sealed class ShipListRow : GuiWidget {
        public Image SpeedIcon;
        public Image ShipIcon;
        public RectTransform[] Slots;
        public Image[] SlotMaterials;
        public CursorHint Click;

        [NonSerialized] public int ShipIndex;
    }

    static public partial class SupplyChainUtility {
        static public void PopulateShipInformation(ShipListRow row, SupplyShipAsset shipAsset, SupplyRouteConfig config) {
            row.ShipIcon.sprite = shipAsset.Icon;

            float speedScale = config.ShipSpeedIconScales[shipAsset.Speed];
            row.SpeedIcon.rectTransform.localScale = new Vector3(speedScale, speedScale, speedScale);

            for (int i = 0; i < row.Slots.Length; i++) {
                row.Slots[i].gameObject.SetActive(i < shipAsset.Capacity);
            }

            for (int i = 0; i < row.SlotMaterials.Length; i++) {
                row.SlotMaterials[i].enabled = false;
            }
        }
    }
}
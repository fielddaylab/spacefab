using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    public sealed class ShipListPanel : SharedPanel, IRegistrationCallbacks {
        public ShipListRow[] Rows;
        public LayoutSizeGroup Layout;
        public LayoutOptions VerticalLayoutOptions;

        [NonSerialized] public ShipListRow SelectedRow;

        void IRegistrationCallbacks.OnDeregister() {
            Game.Events.DeregisterAllForContext(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            SpacefabGame.Events.Register<SupplyRouteEventArgs>(GameEvents.SupplyRouteDrawingOpen, OnRouteStarted)
                .Register<SupplyRouteEventArgs>(GameEvents.SupplyRouteDrawingClose, OnRouteEnded);
        }

        private void OnRouteStarted(SupplyRouteEventArgs evtArgs) {
            SelectedRow = Rows[evtArgs.RouteIndex];
            SelectedRow.LayoutOffset.Offset0 = new Vector2(40, 0);
        }

        private void OnRouteEnded(SupplyRouteEventArgs evtArgs) {
            SelectedRow.LayoutOffset.Offset0 = default;
            SelectedRow = null;
        }
    }

    static public partial class SupplyChainUtility {
        static public void PopulateShipList(ShipListPanel panel, SupplyShipIndex ships, SupplyRouteConfig config) {
            for(int i = 0; i < ships.ShipCount; i++) {
                ShipListRow row = panel.Rows[i];
                PopulateShipInformation(row, ships.ShipAssets[i], config);
                row.Click.UserData = row;
                row.ShipIndex = i;
                row.Click.onClick.Register(HandleShipClicked);
                row.gameObject.SetActive(true);
            }

            for(int i = ships.ShipCount; i < panel.Rows.Length; i++) {
                panel.Rows[i].gameObject.SetActive(false);
            }

            panel.Layout.VerticalLayout(panel.VerticalLayoutOptions);
        }

        static public void HandleShipClicked(PointerListener.EventData evtData) {
            ShipListRow row = (ShipListRow) evtData.Source.UserData;
            Find.State(out SupplyRouteDrawingState draw);
            ShipListPanel panel = (ShipListPanel) row.Panel;

            if (draw.RouteIndex == row.ShipIndex) {
                SupplyRouteUtility.QueueRouteDrawingClose();
            } else {
                SupplyRouteUtility.QueueRouteDrawing(row.ShipIndex);
            }
        }
    }
}
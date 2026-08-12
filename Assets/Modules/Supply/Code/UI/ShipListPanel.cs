using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using FieldDay.World;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Supply {
    public sealed class ShipListPanel : SharedPanel, IRegistrationCallbacks {
        [Serializable]
        public struct SpeedIconConfig {
            public Sprite Image;
            public Vector2 Size;
        }

        public LayoutSizeGroup Layout;
        public LayoutOptions VerticalLayoutOptions;
        public float SelectedRowOffset;

        [Header("Row Config")]
        public ShipListRow[] Rows;
        public SpeedIconConfig[] SpeedIcons;

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
            SelectedRow.LayoutOffset.Offset0 = new Vector2(SelectedRowOffset, 0);
            SelectedRow.CursorHint.TooltipFooter = "<sprite name=\"MouseLeft\"> Cancel";
            SelectedRow.CursorHint.MarkDirty();
            SupplyChainUtility.SetShipRowStatsActive(SelectedRow, true);
            SupplyChainUtility.SyncShipRowPositions(SelectedRow);
            SupplyChainUtility.ReflowShipList(this, true);
        }

        private void OnRouteEnded(SupplyRouteEventArgs evtArgs) {
            SelectedRow.CursorHint.TooltipFooter = "<sprite name=\"MouseLeft\"> Draw Route";
            SelectedRow.CursorHint.MarkDirty();
            SelectedRow.LayoutOffset.Offset0 = default;
            SupplyChainUtility.SetShipRowStatsActive(SelectedRow, evtArgs.Stats.Time > 0);
            SupplyChainUtility.SyncShipRowPositions(SelectedRow);
            SelectedRow = null;
            SupplyChainUtility.ReflowShipList(this, true);
        }
    }

    static public partial class SupplyChainUtility {
        static public void PopulateShipList(ShipListPanel panel, SupplyShipIndex ships) {
            for(int i = 0; i < ships.ShipCount; i++) {
                ShipListRow row = panel.Rows[i];
                PopulateShipInformation(row, ships.ShipAssets[i], panel);
                row.CursorHint.Owner = row.CursorHint.UserData = row;
                row.ShipIndex = i;
                row.CursorHint.onClick.Register(HandleShipClicked);
                row.gameObject.SetActive(true);
                SetShipRowStatsActive(row, false);
            }

            for(int i = ships.ShipCount; i < panel.Rows.Length; i++) {
                panel.Rows[i].gameObject.SetActive(false);
            }

            ReflowShipList(panel, true);
        }

        static public void ReflowShipList(ShipListPanel panel, bool snap) {
            using (var children = panel.Layout.Root.QueryLayoutChildren()) {
                var yBuffer = Frame.AllocSpan<float>(children.Count);
                Positioning.DeferredVerticalLayout(children, panel.VerticalLayoutOptions, 0, yBuffer);
                for(int i = 0; i < children.Count; i++) {
                    var row = panel.Rows[i];
                    row.TargetPos.y = yBuffer[i];
                    if (snap) {
                        Positioning.SetOffsetY(row.Rect, row.TargetPos.y);
                        SyncShipRowPositions(row);
                    }
                }
            }
        }

        static public void HandleShipClicked(PointerListener.EventData evtData) {
            ShipListRow row = (ShipListRow) evtData.Source.UserData;
            Find.State(out SupplyRouteDrawingState draw);
            ShipListPanel panel = (ShipListPanel) row.Panel;

            if (draw.RouteIndex == row.ShipIndex) {
                SupplyRouteUtility.QueueRouteDrawingClose();
            } else {
                SupplyRouteUtility.QueueRouteDrawing(row.ShipIndex);

                using (TempVarTable table = TempVarTable.Alloc()) {
                    table.Set("ship", row.ShipIndex);
                    ScriptUtility.Trigger(SupplyScriptTriggers.OnShipSelected, table);
                }
            }
        }
    }
}
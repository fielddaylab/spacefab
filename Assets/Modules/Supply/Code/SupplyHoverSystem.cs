using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.HID;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyHoverSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&UpdateHover, new SysUpdate(GameLoopPhase.LateUpdate, 10, UpdateMasks.SupplyMask),
                new SysPermissions()
                    .ReadWriteShared<SupplyHoverState>());
        }

        static private void UpdateHover(float dt) {
            Find.State(out SupplyHoverState hoverState);

            Vector3? mousePos = null;
            SupplyRouteNode hoverNode = null;
            SupplyRouteRenderer hoverLine = null;

            if (CursorUtility.IsCursorWithinVirtualViewport() && !Game.Input.IsPointerOverCanvas()) {
                mousePos = Game.Rendering.PrimaryCamera.ScreenToWorldPoint(Input.mousePosition);
                GameObject go = Game.Input.CurrentPointerOver();
            }

            hoverState.MousePosition = mousePos;

            if (hoverState.Node != hoverNode) {
                if (hoverState.Node != null) {
                    
                }

                hoverState.Node = hoverNode;

                if (hoverState.Node != hoverNode) {

                }
            }

            if (hoverState.Line != hoverLine) {
                if (hoverState.Line != null) {

                }

                hoverState.Line = hoverLine;

                if (hoverState.Line != null) {

                }
            }
        }
    }
}
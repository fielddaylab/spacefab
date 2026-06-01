using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.UI.Widgets;
using SpaceFab.Materials;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SpaceFab.Supply {
    public sealed class SupplyRouteNodeDetailsDisplay : BatchedComponent {
        public float RadiusPadding = 0.25f;

        [Header("Underlay")]
        public LineRenderer Underlay;
        
        [Header("Time")]
        public GuiCounter TimeCounter;
        public float TimePosition = 0;

        [Header("Cost")]
        public GuiCounter CostCounter;
        public float CostPosition = -0.2f;

        [Header("Risk")]
        public GuiCounter RiskCounter;
        public float RiskPosition = -0.45f;

        [NonSerialized] public float LastKnownRadius;
    }

    static public partial class SupplyRouteUtility {
        static public unsafe void ConfigureNodeDetailsDisplayRadius(SupplyRouteNodeDetailsDisplay display, float radius) {
            if (Mathf.Approximately(radius, display.LastKnownRadius)) {
                return;
            }

            display.LastKnownRadius = radius;

            radius += display.RadiusPadding;

            // position details
            PositionNodeDetailsComponent(display.TimeCounter.Rect, display.TimePosition * Mathf.PI, radius);
            PositionNodeDetailsComponent(display.CostCounter.Rect, display.CostPosition * Mathf.PI, radius);
            PositionNodeDetailsComponent(display.RiskCounter.Rect, display.RiskPosition * Mathf.PI, radius);

            // precision
            float minLineRadians = display.TimePosition * Mathf.PI;
            float maxLineRadians = display.RiskPosition * Mathf.PI;
            float lineRadians = maxLineRadians - minLineRadians;
            int precision = display.Underlay.positionCount - 1;
            float radianIncrement = lineRadians / precision;

            Vector3* positions = stackalloc Vector3[precision + 1];
            for(int i = 0; i <= precision; i++) {
                float radians = minLineRadians + radianIncrement * i;
                positions[i] = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0);
            }

            display.Underlay.SetPositions(Unsafe.NativeArray(positions, precision + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private void PositionNodeDetailsComponent(Transform position, float radians, float radius) {
            position.localPosition = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0);
        }

        static public void PopulateNodeDetailsDisplay(SupplyRouteNodeDetailsDisplay display, SupplyRouteNode node) {
            ConfigureNodeDetailsDisplayRadius(display, PhysicsUtils.GetRadius(node.Collider) * node.Collider.transform.localScale.x);
            display.TimeCounter.SetValue(node.Time);
            display.CostCounter.SetValue(node.Cost);
            display.RiskCounter.SetValue(node.Risk);

            Vector3 localPos = display.transform.localPosition;
            localPos.x = node.Position.x;
            localPos.y = node.Position.y;
            display.transform.localPosition = localPos;
        }
    }
}
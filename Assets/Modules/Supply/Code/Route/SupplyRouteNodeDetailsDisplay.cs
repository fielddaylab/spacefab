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
        public LineRenderer TopUnderlay;
        public Transform TopDot;
        public Transform LeftDot;
        
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
            PositionNodeDetailsComponent(display.TopDot, 0.5f * Mathf.PI, radius);
            PositionNodeDetailsComponent(display.LeftDot, Mathf.PI, radius);

            // precision
            const int pointCount = 20;
            Vector3* positions = stackalloc Vector3[pointCount];
            GenerateCoordinates(positions, pointCount, radius, display.TimePosition * Mathf.PI, display.RiskPosition * Mathf.PI);
            display.Underlay.SetPositions(Unsafe.NativeArray(positions, pointCount));
            GenerateCoordinates(positions, pointCount, radius, 0.5f * Mathf.PI, Mathf.PI);
            display.TopUnderlay.SetPositions(Unsafe.NativeArray(positions, pointCount));
        }

        static private unsafe void GenerateCoordinates(Vector3* positions, int dstCount, float radius, float minRadians, float maxRadians) {
            float lineRadians = maxRadians - minRadians;
            int precision = dstCount - 1;
            float radianIncrement = lineRadians / precision;

            for(int i = 0; i <= precision; i++) {
                float radians = minRadians + radianIncrement * i;
                positions[i] = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0);
            }
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
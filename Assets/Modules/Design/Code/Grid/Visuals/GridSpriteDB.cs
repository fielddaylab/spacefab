using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design.Visuals
{
    [CreateAssetMenu(menuName = "SpaceFab/Design/Grid Sprite DB")]
    public class GridSpriteDB : GlobalAsset
    {
        [Header("Metal")]
        public Sprite Metal;
        public PathLibrary MetalLibrary;

        [Header("Transistors")]
        public Sprite Transistor;
        public PathLibrary TransistorLibrary;
        public Color NColor;
        public Color PColor;
        public Sprite NSide;
        public Sprite PSide;
        public Sprite InvertedOverlay;
        public Sprite InvertedOverlayBase;

        [Header("Vias")]
        public Sprite Via;
        public Sprite ViaHigh;
        public Sprite ViaLow;
        public Sprite ViaUnstable;

        [Header("Gates")]
        public Sprite Gate;
        public Sprite GateHigh;
        public Sprite GateLow;
        public Sprite GateUnstable;

        [Header("IO")]
        public Sprite IOInner;
        public Sprite IOOuter;

        [Header("Flow")]
        public Sprite FlowHiAbove;
        public Sprite FlowLoAbove;
        public Sprite FlowUnstableAbove;
        public Sprite FlowHiBelow;
        public Sprite FlowLoBelow;
        public Sprite FlowUnstableBelow;

        [Header("Input Toggle Overlay")]
        public Sprite InputToggleBackground;
        public Sprite InputToggleArrow;
        public Color InputToggleLoColor = Color.white;
        public Color InputToggleLoTextColor = Color.white;
        public Color InputToggleHiColor = Color.white;
        public Color InputToggleHiTextColor = Color.white;
    }

    public static class GridSpriteDBUtility
    {
        public static Sprite LookupViaSprite(GridSpriteDB spriteDB, FlowState state)
        {
            switch (state)
            {
                case FlowState.Empty:
                    return spriteDB.Via;
                case FlowState.Hi:
                    return spriteDB.ViaHigh;
                case FlowState.Lo:
                    return spriteDB.ViaLow;
                case FlowState.Unstable:
                    return spriteDB.ViaUnstable;
                default:
                    return null;
            }
        }

        public static Sprite LookupGateSprite(GridSpriteDB spriteDB, FlowState state)
        {
            switch (state)
            {
                case FlowState.Empty:
                    return spriteDB.Gate;
                case FlowState.Hi:
                    return spriteDB.GateHigh;
                case FlowState.Lo:
                    return spriteDB.GateLow;
                case FlowState.Unstable:
                    return spriteDB.GateUnstable;
                default:
                    return null;
            }
        }

        // Tint colour applied to the input-toggle overlay's tinted renderers based on the
        // current Lo/Hi state. Falls back to white for non-binary states so a misconfigured
        // entry stays visible.
        public static Color LookupInputToggleColor(GridSpriteDB spriteDB, FlowState state)
        {
            if (spriteDB == null) { return Color.white; }
            if (state == FlowState.Hi) { return spriteDB.InputToggleHiColor; }
            if (state == FlowState.Lo) { return spriteDB.InputToggleLoColor; }
            return Color.white;
        }

        public static Color LookupInputToggleTextColor(GridSpriteDB spriteDB, FlowState state)
        {
            if (spriteDB == null) { return Color.white; }
            if (state == FlowState.Hi) { return spriteDB.InputToggleHiTextColor; }
            if (state == FlowState.Lo) { return spriteDB.InputToggleLoTextColor; }
            return Color.white;
        }
    }
}


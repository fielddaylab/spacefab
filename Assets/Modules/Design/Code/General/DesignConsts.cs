using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public static class DesignConsts
    {
        public static readonly int NUM_GRID_ROWS = 6;
        public static readonly int NUM_GRID_COLS = 8;

        public static Vector2Int EMPTY_DRAG_COORD = -Vector2Int.one;
    }

    public static class DesignScriptTriggers
    {
        public static readonly StringHash32 OnToolSelected =         "OnToolSelected";
        public static readonly StringHash32 OnClickReleased =        "OnClickReleased";
        public static readonly StringHash32 OnDragReleased =         "OnDragReleased";
        public static readonly StringHash32 OnInputToggled =         "OnInputToggled";
        public static readonly StringHash32 OnSingleTestComplete =   "OnSingleTestComplete";
        public static readonly StringHash32 OnAllTestsComplete =     "OnAllTestsComplete";
        // Fires once the Design level finishes setup (grid built, input/output overlays spawned and
        // their ElementTag ids registered). Onboarding scripts that highlight elements should match
        // this rather than OnMinigameLoad, since the tag ids aren't in the lookup until setup ends.
        public static readonly StringHash32 OnDesignSetupComplete =        "OnDesignSetupComplete";
    }
}
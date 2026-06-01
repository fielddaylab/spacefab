using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public enum ToolType : uint
    {
        None,

        // Draw System
        DrawMetal,
        DrawNNodes,
        DrawPNodes,
        DrawVia,
        DrawGate,

        // Erase System
        Erase,
    }


    /// <summary>
    /// Holds data relevant to Tool mode.
    /// Includes tracking current tool.
    /// </summary>
    public class ToolModeState : SharedStateComponent, IRegistrationCallbacks
    {
        public StackLayer ActiveLayer;
        public ToolType ActiveTool;

        public Vector2Int StartingDragCoord;
        public Vector2Int LastKnownDragCoord;
        public Vector2Int LastTerminatedDragCoord;

        public void OnRegister()
        {
            LastTerminatedDragCoord = DesignConsts.EMPTY_DRAG_COORD;
            ActiveTool = ToolType.None;
        }

        public void OnDeregister()
        {

        }
    }

    public static class ToolModeUtility
    {
        #region Releases

        /// <summary>
        /// Terminates drag tracking.
        /// </summary>
        /// <param name="fullRelease">True if player released mouse button, false if released due to logic rules</param>
        public static void TerminateDrag(ToolModeState toolModeState, bool fullRelease = false)
        {
            // stop tracking dragging
            if (!fullRelease) { toolModeState.LastTerminatedDragCoord = toolModeState.LastKnownDragCoord; }
            else {
                if (toolModeState.StartingDragCoord == toolModeState.LastKnownDragCoord) {
                    ScriptUtility.Trigger(DesignScriptTriggers.OnClickReleased);
                }
                else {
                    ScriptUtility.Trigger(DesignScriptTriggers.OnDragReleased);
                }

                toolModeState.LastTerminatedDragCoord = -Vector2Int.one; 
            }

            toolModeState.LastKnownDragCoord = -Vector2Int.one;
            toolModeState.StartingDragCoord = -Vector2Int.one;
        }

        #endregion // Releases
    }
}
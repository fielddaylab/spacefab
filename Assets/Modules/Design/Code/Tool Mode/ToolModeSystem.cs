using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SpaceFab.Design.Visuals;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages Tool mode, in which the player is actively shaping the grid.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhaseMask.Update, 0, UpdateMasks.ToolModeMask)]
    public class ToolModeSystem : SharedStateSystemBehaviour<ToolModeState, GridStackState, VisualGridStackState>
    {
        
    }
}
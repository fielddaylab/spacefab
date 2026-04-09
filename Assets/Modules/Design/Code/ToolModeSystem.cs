using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    /// <summary>
    /// Manages Tool mode, in which the player is actively shaping the grid.
    /// Delegates to DrawSystem or EraseSystem depending on the currently selected tool type.
    /// </summary>
    public class ToolModeSystem : SharedStateSystemBehaviour<ToolModeState>
    {

    }
}
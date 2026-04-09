using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication
{
    /// <summary>
    /// Holds data regarding the microgame precisions in the current attempt.
    /// Includes step-specific and cumulative precision data.
    /// Resets on every attempt.
    /// Will be compared with precision targets loaded into FabricationMinigameState on level load.
    /// Relevant pieces should be passed to FabricationMinigameState after results.
    /// </summary>
    public class PrecisionState : SharedStateComponent
    {

    }
}
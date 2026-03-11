using FieldDay.SharedState;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Delegates Minigame functions to the current minigame
/// </summary>
namespace SpaceFab
{
    public class MinigameStateInterfacer : SharedStateComponent
    {
        public MinigameStateBase MinigameState;
    }
}
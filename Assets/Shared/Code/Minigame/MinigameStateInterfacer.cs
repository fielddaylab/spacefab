using FieldDay.SharedState;
using SpaceFab.Overarching;
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
        public MinigameId Id;
        public MinigameStateBase MinigameState;
    }
}
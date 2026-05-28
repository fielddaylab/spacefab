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

        // Pass-through to the active minigame's runtime FoundValidSolution flag. Returns false
        // if no minigame state is wired up (e.g. before scene init completes).
        public bool HasValidSolution()
        {
            return MinigameState != null && MinigameState.FoundValidSolution;
        }
    }
}
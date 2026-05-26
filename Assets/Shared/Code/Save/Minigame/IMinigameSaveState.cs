using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    public interface IMinigameSaveState
    {
        public void SetDefaults();

        public bool HasValidSolution();
    }
}